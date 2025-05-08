using FacebookGrapQLApi.Models;
using FacebookUserInfoApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("api/[controller]/[action]")]
[AllowAnonymous]
public class FacebookController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly FacebookSettings _fbSettings;

    public FacebookController(IHttpClientFactory factory, IOptions<FacebookSettings> options)
    {
        _httpClient = factory.CreateClient();
        _fbSettings = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetFacebookUser([FromQuery] string? access_token)
    {
        string userAccessToken = access_token;

        // Eğer query string parametresi yoksa, Authorization header'ına bak
        if (string.IsNullOrWhiteSpace(userAccessToken))
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized("Access token query parametresi ya da Authorization header gerekli.");

            userAccessToken = authHeader.Substring("Bearer ".Length).Trim();
        }

        // 1. App token al
        var appTokenUrl = $"https://graph.facebook.com/oauth/access_token" +
                          $"?client_id={_fbSettings.AppId}" +
                          $"&client_secret={_fbSettings.AppSecret}" +
                          $"&grant_type=client_credentials";

        var appTokenResponse = await _httpClient.GetAsync(appTokenUrl);
        if (!appTokenResponse.IsSuccessStatusCode)
            return StatusCode(500, "Facebook app token alınamadı.");

        var appTokenContent = await appTokenResponse.Content.ReadAsStringAsync();
        var appTokenJson = JObject.Parse(appTokenContent);
        var appAccessToken = appTokenJson["access_token"]?.ToString();

        // 2. Token geçerliliğini kontrol et
        var debugUrl = $"https://graph.facebook.com/debug_token" +
                       $"?input_token={userAccessToken}" +
                       $"&access_token={appAccessToken}";

        var debugResponse = await _httpClient.GetAsync(debugUrl);
        if (!debugResponse.IsSuccessStatusCode)
            return Unauthorized("Facebook kullanıcı token'ı geçersiz veya süresi dolmuş.");

        // 3. Kullanıcı bilgilerini al
        var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email&access_token={userAccessToken}";
        var userInfoResponse = await _httpClient.GetAsync(userInfoUrl);
        if (!userInfoResponse.IsSuccessStatusCode)
            return StatusCode(500, "Facebook kullanıcı bilgileri alınamadı.");

        var content = await userInfoResponse.Content.ReadAsStringAsync();
        var json = JObject.Parse(content);

        var user = new FacebookUserDto
        {
            Id = json["id"]?.ToString(),
            Name = json["name"]?.ToString(),
            Email = json["email"]?.ToString()
        };

        return Ok(user);
    }

}
