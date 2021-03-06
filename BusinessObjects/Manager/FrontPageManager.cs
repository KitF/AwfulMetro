﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using AwfulMetro.Core.Entity;
using AwfulMetro.Core.Tools;
using HtmlAgilityPack;

namespace AwfulMetro.Core.Manager
{
    public class FrontPageManager
    {
        private readonly IWebManager _webManager;

        public FrontPageManager(IWebManager webManager)
        {
            _webManager = webManager;
        }

        public FrontPageManager() : this(new WebManager())
        {
        }

        public List<PopularThreadsTrendsEntity> GetPopularThreads(HtmlDocument doc)
        {
            var popularThreadsList = new List<PopularThreadsTrendsEntity>();
            HtmlNode popularThreadNodeList =
                doc.DocumentNode.Descendants("div")
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("popular_threads"));
            foreach (HtmlNode popularThreadNode in popularThreadNodeList.Descendants("li"))
            {
                var popularThread = new PopularThreadsTrendsEntity();
                popularThread.ParseThread(popularThreadNode);
                popularThreadsList.Add(popularThread);
            }
            return popularThreadsList;
        }

        public List<PopularThreadsTrendsEntity> GetPopularTrends(HtmlDocument doc)
        {
            var popularTrendsList = new List<PopularThreadsTrendsEntity>();
            HtmlNode popularTrendsNodeList =
                doc.DocumentNode.Descendants("div")
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("organ whatshot"));
            foreach (HtmlNode popularThreadNode in popularTrendsNodeList.Descendants("li"))
            {
                var popularTrend = new PopularThreadsTrendsEntity();
                popularTrend.ParseTrend(popularThreadNode);
                popularTrendsList.Add(popularTrend);
            }
            return popularTrendsList;
        }

        public List<FrontPageArticleEntity> GetFrontPageArticles(HtmlDocument doc)
        {
            var frontPageArticleList = new List<FrontPageArticleEntity>();
            HtmlNode frontPageNode =
                doc.DocumentNode.Descendants("div")
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("main_article"));
            var mainArticle = new FrontPageArticleEntity();
            mainArticle.ParseMainArticle(frontPageNode);
            frontPageArticleList.Add(mainArticle);
            frontPageNode =
                doc.DocumentNode.Descendants("ul")
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("news"));
            foreach (HtmlNode frontPageNewsArticle in frontPageNode.Descendants("li"))
            {
                var article = new FrontPageArticleEntity();
                article.ParseFrontPageArticle(frontPageNewsArticle);
                frontPageArticleList.Add(article);
            }
            return frontPageArticleList;
        }

        public List<FrontPageArticleEntity> GetFeatures(HtmlDocument doc)
        {
            var frontPageFeatureList = new List<FrontPageArticleEntity>();
            HtmlNode frontPageNode =
                doc.DocumentNode.Descendants("ul")
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("featured"));
            foreach (HtmlNode frontPageFeature in frontPageNode.Descendants("li"))
            {
                var article = new FrontPageArticleEntity();
                article.ParseFeatured(frontPageFeature);
                frontPageFeatureList.Add(article);
            }
            return frontPageFeatureList;
        }

        public async Task<FrontPageWebArticleEntity> GetFrontPageArticle(string url)
        {
            HtmlDocument articleDoc = (await _webManager.DownloadHtml(url)).Document;
            HtmlNode articleNode =
                articleDoc.DocumentNode.Descendants()
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("cavity left"));
            HtmlNode articleBodyNode =
                articleNode.Descendants()
                    .FirstOrDefault(node => node.GetAttributeValue("class", string.Empty).Contains("organ article"));

            string html = await PathIO.ReadTextAsync("ms-appx:///Assets/MainSite.html");

            var doc2 = new HtmlDocument();

            doc2.LoadHtml(html);

            HtmlNode bodyNode = doc2.DocumentNode.Descendants("body").FirstOrDefault();

            bodyNode.InnerHtml = articleBodyNode.OuterHtml;

            var frontPageArticleEntity = new FrontPageWebArticleEntity();
            frontPageArticleEntity.MapTo(WebUtility.HtmlDecode(doc2.DocumentNode.OuterHtml), 1);
            return frontPageArticleEntity;
        }

        public async Task<HtmlDocument> GetFrontPage()
        {
            return (await _webManager.DownloadHtml(Constants.FRONT_PAGE)).Document;
        }
    }
}