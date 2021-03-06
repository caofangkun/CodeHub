﻿using System;
using Xamarin.Utilities.Core.Services;
using CodeHub.Core.Models;
using System.Threading.Tasks;
using System.Net.Http;

namespace CodeHub.Core.Services
{
    public class ImgurService : IImgurService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly IJsonSerializationService _jsonSerializationService;
        private const string AuthorizationClientId = "4d2779fd2cc56cb";
        private const string ImgurPostUrl = "https://api.imgur.com/3/image";

        public ImgurService(IHttpClientService httpClientService, IJsonSerializationService jsonSerializationService)
        {
            _httpClientService = httpClientService;
            _jsonSerializationService = jsonSerializationService;
        }

        public async Task<ImgurModel> SendImage(byte[] data)
        {
            var client = _httpClientService.Create();
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.Add("Authorization", "Client-ID " + AuthorizationClientId);
            var body = _jsonSerializationService.Serialize(new { image = Convert.ToBase64String(data) });
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(ImgurPostUrl, content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Unable to post to Imgur! " + response.ReasonPhrase);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return _jsonSerializationService.Deserialize<ImgurModel>(responseBody);
        }
    }
}

