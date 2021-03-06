﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using AwfulMetro.Core.Entity;
using AwfulMetro.Core.Tools;
using HtmlAgilityPack;

namespace AwfulMetro.Core.Manager
{
    public class ThreadManager
    {
        private readonly IWebManager _webManager;

        public ThreadManager(IWebManager webManager)
        {
            _webManager = webManager;
        }

        public ThreadManager() : this(new WebManager())
        {
        }

        public async Task<string> GetThread(ForumThreadEntity forumThread)
        {
            string url = string.Format(Constants.REPLY_BASE, forumThread.ThreadId);
            WebManager.Result result = await _webManager.DownloadHtml(url);
            HtmlDocument doc = result.Document;
            return await GetThreadHtml(doc);
        }

        public async Task<string> GetThreadHtml(HtmlDocument doc)
        {
            string html = await PathIO.ReadTextAsync("ms-appx:///Assets/thread.html");

            var doc2 = new HtmlDocument();

            doc2.LoadHtml(html);

            HtmlNode bodyNode = doc2.DocumentNode.Descendants("body").FirstOrDefault();

            HtmlNode[] replyNodes = doc.DocumentNode.Descendants("div").ToArray();

            HtmlNode threadNode = replyNodes.FirstOrDefault(node => node.GetAttributeValue("id", "").Equals("thread"));

            int threadId = ParseInt(threadNode.GetAttributeValue("class", string.Empty));

            IEnumerable<HtmlNode> postNodes =
                threadNode.Descendants("table")
                    .Where(node => node.GetAttributeValue("class", string.Empty).Equals("post "));

            foreach (HtmlNode post in postNodes)
            {
                HtmlNode userInfoNode =
                    post.Descendants("td")
                        .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("userinfo"));

                int userId = ParseInt(userInfoNode.GetAttributeValue("class", string.Empty));

                int postId = ParseInt(post.GetAttributeValue("id", string.Empty));

                //TODO: Create HTML Render engine to handle this, rather than hard coding. That way it can be used for the front page or other web views.

                HtmlNode profileLinksNode =
                    post.Descendants("td")
                        .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Equals("postlinks"));
                bool isCurrentUserPost =
                    profileLinksNode.Descendants("img")
                        .FirstOrDefault(node => node.GetAttributeValue("alt", string.Empty).Equals("Edit")) != null;
                profileLinksNode.InnerHtml = string.Empty;

                string profileButton =
                    WebUtility.HtmlDecode(
                        string.Format(
                            "<li><button onclick=\"window.ForumCommand('profile', '{0}');\">Profile</button></li>",
                            userId));

                string postHistoryButton =
                    WebUtility.HtmlDecode(
                        string.Format(
                            "<li><button onclick=\"window.ForumCommand('post_history', '{0}');\">Post History</button></li>",
                            userId));

                string rapSheetButton =
                    WebUtility.HtmlDecode(
                        string.Format(
                            "<li><button onclick=\"window.ForumCommand('rap_sheet', '{0}');\">Rap Sheet</button></li>",
                            userId));

                string quoteButton =
                    WebUtility.HtmlDecode(
                        string.Format(
                            "<li><button onclick=\"window.ForumCommand('quote', '{0}');\">Quote</button></li>", postId));

                string editButton =
                    WebUtility.HtmlDecode(
                        string.Format("<li><button onclick=\"window.ForumCommand('edit', '{0}');\">Edit</button></li>",
                            postId));

                profileLinksNode.InnerHtml = isCurrentUserPost
                    ? string.Concat("<ul class=\"profilelinks\">", profileButton, postHistoryButton, rapSheetButton,
                        quoteButton, editButton, "</ul>")
                    : string.Concat("<ul class=\"profilelinks\">", profileButton, postHistoryButton, rapSheetButton,
                        quoteButton, "</ul>");

                HtmlNode postDateNode = post.Descendants("td")
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Equals("postdate"));
                string postDate = postDateNode.InnerText;

                string toPostButton =
                    WebUtility.HtmlDecode(
                        string.Format(
                            "<button style=\"min-width:20px;display: inline-block;\" onclick=\"window.TopPost(#{0});\">#</button>",
                            postId));

                string usersPostsInThreadButton =
                    WebUtility.HtmlDecode(
                        string.Format(
                            "<button style=\"min-width:20px;display: inline-block;\" onclick=\"window.ForumCommand('users_posts_in_thread', '{0},{1}');\">?</button>",
                            userId, threadId));

                //postDateNode.InnerHtml = string.Concat("<div style=\"display: inline-block;\">", toPostButton, usersPostsInThreadButton, "</div>", postDate);
                postDateNode.InnerHtml = postDate;
            }

            bodyNode.InnerHtml = threadNode.OuterHtml;

            return WebUtility.HtmlDecode(doc2.DocumentNode.OuterHtml);
        }

        private int ParseInt(string postClass)
        {
            string re1 = ".*?"; // Non-greedy match on filler
            string re2 = "(\\d+)"; // Integer Number 1

            var r = new Regex(re1 + re2, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m = r.Match(postClass);
            if (!m.Success) return 0;
            String int1 = m.Groups[1].ToString();
            return Convert.ToInt32(int1);
        }

        public async Task<List<ForumThreadEntity>> GetBookmarks(ForumEntity forumCategory, int page)
        {
            var forumSubcategoryList = new List<ForumEntity>();
            var forumThreadList = new List<ForumThreadEntity>();
            String url = Constants.BOOKMARKS_URL;
            if (forumCategory.CurrentPage > 0)
            {
                url = Constants.BOOKMARKS_URL + string.Format(Constants.PAGE_NUMBER, page);
            }

            HtmlDocument doc = (await _webManager.DownloadHtml(url)).Document;
            HtmlNode forumNode =
                doc.DocumentNode.Descendants()
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("threadlist"));


            foreach (
                HtmlNode threadNode in
                    forumNode.Descendants("tr")
                        .Where(node => node.GetAttributeValue("class", string.Empty).StartsWith("thread")))
            {
                var threadEntity = new ForumThreadEntity();
                threadEntity.Parse(threadNode);
                forumThreadList.Add(threadEntity);
            }
            return forumThreadList;
        }

        public async Task<bool> AddBookmarks(List<long> threadIdList)
        {
            foreach (long threadId in threadIdList)
            {
                await _webManager.PostData(
                    Constants.BOOKMARK, string.Format(
                        Constants.ADD_BOOKMARK, threadId
                        ));
            }
            return true;
        }

        public async Task<bool> RemoveBookmarks(List<long> threadIdList)
        {
            foreach (long threadId in threadIdList)
            {
                await _webManager.PostData(
                    Constants.BOOKMARK, string.Format(
                        Constants.REMOVE_BOOKMARK, threadId
                        ));
            }
            return true;
        }

        public async Task<bool> MarkThreadUnread(List<long> threadIdList)
        {
            foreach (long threadId in threadIdList)
            {
                await _webManager.PostData(
                    Constants.RESET_BASE, string.Format(
                        Constants.RESET_SEEN, threadId
                        ));
            }
            return true;
        }

        public async Task<List<ForumThreadEntity>> GetForumThreads(ForumEntity forumCategory, int page)
        {
            // TODO: Remove parsing logic from managers. I don't think they have a place here...
            string url = forumCategory.Location + string.Format(Constants.PAGE_NUMBER, page);

            HtmlDocument doc = (await _webManager.DownloadHtml(url)).Document;

            HtmlNode forumNode =
                doc.DocumentNode.Descendants()
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("threadlist"));
            var forumThreadList = new List<ForumThreadEntity>();
            foreach (
                HtmlNode threadNode in
                    forumNode.Descendants("tr")
                        .Where(node => node.GetAttributeValue("class", string.Empty).StartsWith("thread")))
            {
                var threadEntity = new ForumThreadEntity();
                threadEntity.Parse(threadNode);
                forumThreadList.Add(threadEntity);
            }
            return forumThreadList;
        }
    }
}