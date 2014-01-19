﻿using System.Linq;
using System.Threading.Tasks;
using AwfulMetro.Core.Entity;
using AwfulMetro.Core.Tools;

namespace AwfulMetro.Core.Manager
{
    public class ForumUserManager
    {
        private readonly IWebManager _webManager;
        public ForumUserManager(IWebManager webManager)
        {
            this._webManager = webManager;
        }

        public ForumUserManager() : this(new WebManager()) { }

        public async Task<ForumUserEntity> GetUserFromProfilePage(long userId)
        {
            string url = Constants.BASE_URL + string.Format(Constants.USER_PROFILE, userId);
            var doc = (await _webManager.DownloadHtml(url)).Document;
            
            /*Get the user profile HTML from the user profile page,
             once we get it, get the nodes for each section of the page and parse them.*/

            var profileNode = doc.DocumentNode.Descendants("td")
                .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("info"));

            var threadNode = doc.DocumentNode.Descendants("td")
                .FirstOrDefault(node => node.GetAttributeValue("id", string.Empty).Contains("thread"));
            var userEntity = ForumUserEntity.FromUserProfile(profileNode, threadNode);
            
            userEntity.FromUserProfileAvatarInformation(threadNode);

            return userEntity;
        }
    }
}
