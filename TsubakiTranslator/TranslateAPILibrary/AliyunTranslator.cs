﻿using RestSharp;
using RestSharp.Serializers;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace TsubakiTranslator.TranslateAPILibrary
{
    //API文档介绍 https://help.aliyun.com/document_detail/158269.html

    class AliyunTranslator : ITranslator
    {
        private readonly string name = "阿里云";
        public string Name { get => name; }

        public string SourceLanguage { get; set; }

        private string SecretId { get; set; }
        private string SecretKey { get; set; }

        public string Translate(string sourceText)
        {
            string desLang = "zh";

            string method = "POST";
            string accept = "application/json";
            //string contentType = "application/json";
            string date = DateTime.UtcNow.ToString("r");
            string host = "mt.cn-hangzhou.aliyuncs.com";
            string path = "/api/translate/web/general";


            var body = new
            {
                FormatType = "text",
                Scene = "general",
                SourceLanguage = SourceLanguage,
                SourceText = sourceText,
                TargetLanguage = desLang,
            };

            string bodyString = JsonSerializer.Serialize(body);
            string bodyMd5 = string.Empty;
            using (MD5 md5Hash = MD5.Create())
            {
                // 将输入字符串转换为字节数组并计算哈希数据  
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(bodyString));
                // 返回BASE64字符串  
                bodyMd5 = Convert.ToBase64String(data);
            }

            string uuid = Guid.NewGuid().ToString();

            string stringToSign = method + "\n" + accept + "\n" + bodyMd5 + "\n" + host + "\n" + date + "\n"
                    + "x-acs-signature-method:HMAC-SHA1\n"
                    + "x-acs-signature-nonce:" + uuid + "\n"
                    + path;

            string signature = string.Empty;
            using (HMACSHA1 mac = new HMACSHA1(Encoding.UTF8.GetBytes(SecretKey)))
            {
                byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                signature = Convert.ToBase64String(hash);
            }

            string authHeader = "acs " + SecretId + ":" + signature;

            var client = new RestClient($"https://{host}");
            var request = new RestRequest(path,Method.Post);
            request.AddHeader("Authorization", authHeader);
            request.AddHeader("Accept", accept);
            request.AddHeader("Content-MD5", bodyMd5);
            request.AddHeader("Content-Type", host);
            request.AddHeader("Date", date);
            request.AddHeader("x-acs-signature-method", "HMAC-SHA1");
            request.AddHeader("x-acs-signature-nonce", uuid);

            request.AddStringBody(bodyString,DataFormat.Json);


            var response = client.Execute(request);

            Regex reg = new Regex(@"""Translated"":""(.*?)""");
            Match match = reg.Match(response.Content);
            string result = match.Groups[1].Value;
            return result;

        }

        public void TranslatorInit(string param1, string param2)
        {
            SecretId = param1;
            SecretKey = param2;
        }
    }
}
