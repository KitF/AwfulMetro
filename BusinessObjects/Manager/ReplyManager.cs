﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AwfulMetro.Core.Entity;
using AwfulMetro.Core.Tools;
using HtmlAgilityPack;

namespace AwfulMetro.Core.Manager
{
    public class ReplyManager
    {
        private readonly IWebManager _webManager;

        public ReplyManager() : this(new WebManager())
        {
        }

        public ReplyManager(IWebManager webManager)
        {
            _webManager = webManager;
        }

        public async Task<ForumReplyEntity> GetReplyCookies(ForumThreadEntity forumThread)
        {
            try
            {
                string url = string.Format(Constants.REPLY_BASE, forumThread.ThreadId);
                WebManager.Result result = await _webManager.DownloadHtml(url);
                HtmlDocument doc = result.Document;

                HtmlNode[] formNodes = doc.DocumentNode.Descendants("input").ToArray();

                HtmlNode formKeyNode =
                    formNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("formkey"));

                HtmlNode formCookieNode =
                    formNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("form_cookie"));

                HtmlNode bookmarkNode =
                    formNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("bookmark"));

                HtmlNode[] textAreaNodes = doc.DocumentNode.Descendants("textarea").ToArray();

                HtmlNode textNode =
                    textAreaNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("message"));

                HtmlNode threadIdNode =
                    formNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("threadid"));

                var threadManager = new ThreadManager();

                string htmlThread = await threadManager.GetThreadHtml(doc);

                var forumReplyEntity = new ForumReplyEntity();
                try
                {
                    string formKey = formKeyNode.GetAttributeValue("value", "");
                    string formCookie = formCookieNode.GetAttributeValue("value", "");
                    string quote = WebUtility.HtmlDecode(textNode.InnerText);
                    string threadId = threadIdNode.GetAttributeValue("value", "");
                    forumReplyEntity.MapThreadInformation(formKey, formCookie, quote, threadId);
                    forumReplyEntity.PreviousPostsRaw = htmlThread;
                    return forumReplyEntity;
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Could not parse newReply form data.");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<ForumReplyEntity> GetReplyCookiesForEdit(long postId)
        {
            try
            {
                string url = string.Format(Constants.EDIT_BASE, postId);
                WebManager.Result result = await _webManager.DownloadHtml(url);
                HtmlDocument doc = result.Document;

                HtmlNode[] formNodes = doc.DocumentNode.Descendants("input").ToArray();

                HtmlNode bookmarkNode =
                    formNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("bookmark"));

                HtmlNode[] textAreaNodes = doc.DocumentNode.Descendants("textarea").ToArray();

                HtmlNode textNode =
                    textAreaNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("message"));

                var threadManager = new ThreadManager();

                //Get previous posts from quote page.
                string url2 = string.Format(Constants.QUOTE_BASE, postId);
                WebManager.Result result2 = await _webManager.DownloadHtml(url2);
                HtmlDocument doc2 = result2.Document;

                string htmlThread = await threadManager.GetThreadHtml(doc2);

                var forumReplyEntity = new ForumReplyEntity();
                try
                {
                    string quote = WebUtility.HtmlDecode(textNode.InnerText);
                    forumReplyEntity.PreviousPostsRaw = htmlThread;
                    string bookmark = bookmarkNode.OuterHtml.Contains("checked") ? "yes" : "no";
                    forumReplyEntity.MapEditPostInformation(quote, postId, bookmark);
                    return forumReplyEntity;
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Could not parse newReply form data.");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<ForumReplyEntity> GetReplyCookies(long postId)
        {
            try
            {
                string url = string.Format(Constants.QUOTE_BASE, postId);
                WebManager.Result result = await _webManager.DownloadHtml(url);
                HtmlDocument doc = result.Document;

                HtmlNode[] formNodes = doc.DocumentNode.Descendants("input").ToArray();

                HtmlNode formKeyNode =
                    formNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("formkey"));

                HtmlNode formCookieNode =
                    formNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("form_cookie"));

                HtmlNode[] textAreaNodes = doc.DocumentNode.Descendants("textarea").ToArray();

                HtmlNode textNode =
                    textAreaNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("message"));

                HtmlNode threadIdNode =
                    formNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("threadid"));

                var threadManager = new ThreadManager();

                string htmlThread = await threadManager.GetThreadHtml(doc);

                var forumReplyEntity = new ForumReplyEntity();
                try
                {
                    string formKey = formKeyNode.GetAttributeValue("value", "");
                    string formCookie = formCookieNode.GetAttributeValue("value", "");
                    string quote = WebUtility.HtmlDecode(textNode.InnerText);
                    string threadId = threadIdNode.GetAttributeValue("value", "");
                    forumReplyEntity.PreviousPostsRaw = htmlThread;
                    forumReplyEntity.MapThreadInformation(formKey, formCookie, quote, threadId);
                    return forumReplyEntity;
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Could not parse newReply form data.");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string HtmlEncode(string text)
        {
            //TODO: Taken directly off of the internet to fix Unicode support. Need to enhance.
            char[] chars = text.ToCharArray();
            var result = new StringBuilder(text.Length + (int) (text.Length*0.1));

            foreach (char c in chars)
            {
                int value = Convert.ToInt32(c);
                if (value > 127)
                    result.AppendFormat("&#{0};", value);
                else
                    result.Append(c);
            }

            result.Replace("&quot;", "\"");
            return result.ToString();
        }


        public async Task<string> CreatePreviewEditPost(ForumReplyEntity forumReplyEntity)
        {
            if (forumReplyEntity == null)
                return string.Empty;
            var form = new MultipartFormDataContent
            {
                {new StringContent("updatepost"), "action"},
                {new StringContent(forumReplyEntity.PostId.ToString()), "postid"},
                {new StringContent(HtmlEncode(forumReplyEntity.Message)), "message"},
                {new StringContent(forumReplyEntity.ParseUrl.ToString()), "parseurl"},
                {new StringContent("2097152"), "MAX_FILE_SIZE"},
                {new StringContent("Preview Post"), "preview"}
            };
            HttpResponseMessage response = await _webManager.PostFormData(Constants.EDIT_POST, form);
            Stream stream = await response.Content.ReadAsStreamAsync();
            using (var reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                HtmlNode[] replyNodes = doc.DocumentNode.Descendants("div").ToArray();

                HtmlNode previewNode =
                    replyNodes.FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("inner postbody"));
                return previewNode == null ? string.Empty : FixPostHtml(previewNode.OuterHtml);
            }
        }

        public async Task<string> CreatePreviewPost(ForumReplyEntity forumReplyEntity)
        {
            if (forumReplyEntity == null)
                return string.Empty;
            var form = new MultipartFormDataContent
            {
                {new StringContent("postreply"), "action"},
                {new StringContent(forumReplyEntity.ThreadId), "threadid"},
                {new StringContent(forumReplyEntity.FormKey), "formkey"},
                {new StringContent(forumReplyEntity.FormCookie), "form_cookie"},
                {new StringContent(HtmlEncode(forumReplyEntity.Message)), "message"},
                {new StringContent(forumReplyEntity.ParseUrl.ToString()), "parseurl"},
                {new StringContent("2097152"), "MAX_FILE_SIZE"},
                {new StringContent("Submit Reply"), "submit"},
                {new StringContent("Preview Reply"), "preview"}
            };
            HttpResponseMessage response = await _webManager.PostFormData(Constants.NEW_REPLY, form);
            Stream stream = await response.Content.ReadAsStreamAsync();
            using (var reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                HtmlNode[] replyNodes = doc.DocumentNode.Descendants("div").ToArray();

                HtmlNode previewNode =
                    replyNodes.FirstOrDefault(node => node.GetAttributeValue("class", "").Equals("inner postbody"));
                return previewNode == null ? string.Empty : FixPostHtml(previewNode.OuterHtml);
            }
        }

        private static string FixPostHtml(String postHtml)
        {
            return "<!DOCTYPE html><html>" + Constants.HTML_HEADER + "<body>" + postHtml + "</body></html>";
        }

        public async Task<bool> SendPost(ForumReplyEntity forumReplyEntity)
        {
            if (forumReplyEntity == null)
                return false;
            var form = new MultipartFormDataContent
            {
                {new StringContent("postreply"), "action"},
                {new StringContent(forumReplyEntity.ThreadId), "threadid"},
                {new StringContent(forumReplyEntity.FormKey), "formkey"},
                {new StringContent(forumReplyEntity.FormCookie), "form_cookie"},
                {new StringContent(HtmlEncode(forumReplyEntity.Message)), "message"},
                {new StringContent(forumReplyEntity.ParseUrl.ToString()), "parseurl"},
                {new StringContent("2097152"), "MAX_FILE_SIZE"},
                {new StringContent("Submit Reply"), "submit"}
            };
            HttpResponseMessage response = await _webManager.PostFormData(Constants.NEW_REPLY, form);

            return true;
        }

        public async Task<bool> SendUpdatePost(ForumReplyEntity forumReplyEntity)
        {
            if (forumReplyEntity == null)
                return false;
            var form = new MultipartFormDataContent
            {
                {new StringContent("updatepost"), "action"},
                {new StringContent(forumReplyEntity.PostId.ToString()), "postid"},
                {new StringContent(HtmlEncode(forumReplyEntity.Message)), "message"},
                {new StringContent(forumReplyEntity.ParseUrl.ToString()), "parseurl"},
                {new StringContent(forumReplyEntity.Bookmark), "bookmark"},
                {new StringContent("2097152"), "MAX_FILE_SIZE"},
                {new StringContent("Save Changes"), "submit"}
            };
            HttpResponseMessage response = await _webManager.PostFormData(Constants.EDIT_POST, form);

            return true;
        }

        public async Task<string> GetQuoteString(long postId)
        {
            string url = string.Format(Constants.QUOTE_BASE, postId);
            WebManager.Result result = await _webManager.DownloadHtml(url);
            HtmlDocument doc = result.Document;

            HtmlNode[] textAreaNodes = doc.DocumentNode.Descendants("textarea").ToArray();

            HtmlNode textNode =
                textAreaNodes.FirstOrDefault(node => node.GetAttributeValue("name", "").Equals("message"));

            try
            {
                return WebUtility.HtmlDecode(textNode.InnerText);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Could not parse newReply form data.");
            }
        }
    }
}