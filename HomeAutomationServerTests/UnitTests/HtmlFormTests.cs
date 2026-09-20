using De.Hochstaetter.HomeAutomationServer.Services.SolarWeb;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The two pages of the Fronius login as <see cref="HtmlForm"/> reads them: the login form of the WSO2 identity
/// server (cut down from the live page of 2026-09-20) and the page that posts the authorization code back to
/// Solar.web.
/// </summary>
public sealed class HtmlFormTests
{
    private const string LoginPage = """
        <!doctype html>
        <html><body>
        <form class="reset-input fro-form" action="../commonauth" method="post" id="loginForm">
            <input type="hidden" name="authenticators" value="SAMLSSOAuthenticator:Fronius Login;FroniusBasicAuthenticator:LOCAL">
            <input type="hidden" name="tenantDomain" value="carbon.super">
            <input type="hidden" name="allLoginParams" value="client_id=mf_o9iTAyKemNLQTa6Sp6HYonCIa&amp;scope=openid+profile">
            <div class="alert alert-danger" style="display: none;" id="error-msg"></div>
            <input
                    type="text"
                    name="usernameUserInput"
                    id="usernameUserInput"
                    value=""
                    patternName="usernameRecoveryPattern"
            />
            <input id="username" name="username" type="hidden" value="null">
            <input class="fro-input" type="password" id="password" name="password" value="" autocomplete="off" />
            <button class="fro-button" type="button" onclick="showPassword('password', 'password-toggle-icon')">eye</button>
            <input class="fro-checkbox" type="checkbox" id="chkRemember" name="chkRemember">
            <input type="hidden" name="sessionDataKey" value='908058f4-200b-4c97-89c8-3719e69f743c'/>
            <button id="login-button" type="submit" class="fro-button">Login</button>
            <input type="submit" name="ignored" value="Login">
        </form>
        </body></html>
        """;

    private const string FormPostPage = """
        <html><head><title>Submit This Form</title></head>
        <body onload="javascript:document.forms[0].submit()">
        <p>You are now redirected back to https://www.solarweb.com/Account/ExternalLoginCallback. If the redirection fails, please click the post button.</p>
        <form method="post" action="https://www.solarweb.com/Account/ExternalLoginCallback">
            <input type="hidden" name="code" value="0b2a4c3c-1d0d-3e71-9c2a-3d1f9c5c9a11"/>
            <input type="hidden" name="id_token" value="eyJ4NXQiOiJabVE0.eyJpc3MiOiJodHRwczovL2xvZ2luLmZyb25pdXMuY29tIn0.c2ln"/>
            <input type="hidden" name="state" value="OpenIdConnect.AuthenticationProperties=Nyzs&#43;QZS8-QSRT"/>
            <input type="hidden" name="session_state" value="ab12"/>
            <button type="submit">POST</button>
        </form>
        </body></html>
        """;

    [Fact]
    public void The_login_form_yields_its_hidden_fields_and_its_relative_action_and_leaves_buttons_and_unchecked_boxes_out()
    {
        var form = Assert.Single(HtmlForm.Parse(LoginPage));

        Assert.Equal("../commonauth", form.Action);
        Assert.Equal(["authenticators", "tenantDomain", "allLoginParams", "usernameUserInput", "username", "password", "sessionDataKey"], form.Fields.Select(f => f.Key));
        Assert.Equal("SAMLSSOAuthenticator:Fronius Login;FroniusBasicAuthenticator:LOCAL", form["authenticators"]);
        Assert.Equal("client_id=mf_o9iTAyKemNLQTa6Sp6HYonCIa&scope=openid+profile", form["allLoginParams"]);
        Assert.Equal("null", form["username"]);
        Assert.Equal("908058f4-200b-4c97-89c8-3719e69f743c", form["sessionDataKey"]);
        Assert.True(form.Has("sessionDataKey"));
        Assert.False(form.Has("chkRemember"));
        Assert.False(form.Has("ignored"));
        Assert.False(form.Has("code"));

        Assert.Equal(new Uri("https://login.fronius.com/commonauth"), new Uri(new Uri("https://login.fronius.com/authenticationendpoint/login.do?client_id=x&sessionDataKey=y"), form.Action));
    }

    [Fact]
    public async Task The_content_carries_the_fields_with_the_typed_values_replacing_the_hidden_ones()
    {
        var form = Assert.Single(HtmlForm.Parse(LoginPage));

        var body = await form.ToContent(
            new KeyValuePair<string, string>("username", "you@example.com"),
            new KeyValuePair<string, string>("password", "p&ss wörd"),
            new KeyValuePair<string, string>("chkRemember", "on")).ReadAsStringAsync(TestContext.Current.CancellationToken);

        var pairs = body.Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1].Replace('+', ' ')));

        Assert.Equal("you@example.com", pairs["username"]);
        Assert.Equal("p&ss wörd", pairs["password"]);
        Assert.Equal("on", pairs["chkRemember"]);
        Assert.Equal("908058f4-200b-4c97-89c8-3719e69f743c", pairs["sessionDataKey"]);
        Assert.Equal("carbon.super", pairs["tenantDomain"]);
        Assert.DoesNotContain("null", body);
    }

    [Fact]
    public void The_form_post_page_yields_the_code_the_token_and_the_state_with_entities_decoded()
    {
        var form = Assert.Single(HtmlForm.Parse(FormPostPage));

        Assert.Equal("https://www.solarweb.com/Account/ExternalLoginCallback", form.Action);
        Assert.Equal(["code", "id_token", "state", "session_state"], form.Fields.Select(f => f.Key));
        Assert.Equal("0b2a4c3c-1d0d-3e71-9c2a-3d1f9c5c9a11", form["code"]);
        Assert.Equal("OpenIdConnect.AuthenticationProperties=Nyzs+QZS8-QSRT", form["state"]);
        Assert.True(form.Has("code"));
        Assert.False(form.Has("sessionDataKey"));
    }

    [Fact]
    public void A_summary_is_the_title_and_the_text_without_markup_scripts_and_styles_cut_to_length()
    {
        const string page = "<html><head><title> 500 - Internal server error. </title><style>p {margin: 0}</style></head><body><script>window.x = 1;</script><p>There is a problem with the resource you are looking for, &amp; it cannot be displayed.</p>\n\n  <p>Second   line</p></body></html>";

        Assert.Equal("'500 - Internal server error.': There is a problem with the resource you are looking for, & it cannot be displayed. Second line", HtmlForm.Summarize(page, 400));
        Assert.Equal("'500 - Internal server error.': There…", HtmlForm.Summarize(page, 38));
        Assert.Equal("just text", HtmlForm.Summarize("just text", 400));
    }

    [Fact]
    public void A_page_without_a_form_yields_none_and_a_checked_box_without_a_value_sends_on()
    {
        Assert.Empty(HtmlForm.Parse("<html><body><p>500 - Internal server error.</p></body></html>"));

        var form = Assert.Single(HtmlForm.Parse("<FORM ACTION='/x'><INPUT TYPE=checkbox NAME=remember CHECKED><input name='a' value='1'><input name='a' value='2'></FORM>"));
        Assert.Equal("/x", form.Action);
        Assert.Equal("on", form["remember"]);
        Assert.Equal(2, form.Fields.Count(f => f.Key == "a"));
    }
}
