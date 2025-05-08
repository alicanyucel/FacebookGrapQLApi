using FacebookGrapQLApi.Models;
using FacebookUserInfoApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("api/[controller]/[action]")]
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
    public async Task<IActionResult> GetFacebookUser([FromQuery] string userAccessToken)
    {
        if (string.IsNullOrWhiteSpace(userAccessToken))
            return BadRequest("User access token is required");
        var appTokenUrl = $"https://graph.facebook.com/oauth/access_token" +
                          $"?client_id={_fbSettings.AppId}" +
                          $"&client_secret={_fbSettings.AppSecret}" +
                          $"&grant_type=client_credentials";

        var appTokenResponse = await _httpClient.GetAsync(appTokenUrl);
        var appTokenContent = await appTokenResponse.Content.ReadAsStringAsync();
        var appTokenJson = JObject.Parse(appTokenContent);
        var appAccessToken = appTokenJson["access_token"]?.ToString();
        var debugUrl = $"https://graph.facebook.com/debug_token" +
                       $"?input_token={userAccessToken}" +
                       $"&access_token={appAccessToken}";

        var debugResponse = await _httpClient.GetAsync(debugUrl);
        if (!debugResponse.IsSuccessStatusCode)
            return Unauthorized("Invalid or expired Facebook user access token.");
        var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email&access_token={userAccessToken}";
        var userInfoResponse = await _httpClient.GetAsync(userInfoUrl);
        var content = await userInfoResponse.Content.ReadAsStringAsync();
        var json = JObject.Parse(content);

        var user = new FacebookUserDto
        {
            Id = json["id"]?.ToString(),
            Name = json["name"]?.ToString(),
            Email = json["email"]?.ToString()
        };
        // bakııym ....
        return Ok(user);
    }
}
