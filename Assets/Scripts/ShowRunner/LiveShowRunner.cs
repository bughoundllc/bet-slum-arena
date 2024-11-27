using System;
using UnityEngine;

namespace bet_slum.showRunner
{
    public class LiveShowRunner: ShowRunner
    {
        private const string GAME_CONTROLLER_EMAIL = "0de94eda24d841a09305140b73dae246@net.slum";
        private const string GAME_CONTROLLER_PW = "TnzS$gQLk5NveIen";
        private static string _sessionToken = "";

        protected async override Awaitable Initialize()
        {
            await LoginToAPI();
            await LoadGame();
            InitializeMatch();
            await Awaitable.WaitForSecondsAsync(0.5f);
            StartMatch();
        }

        private async Awaitable LoginToAPI()
        {
            var loginForm = new WWWForm();
            loginForm.AddField("email", GAME_CONTROLLER_EMAIL);
            loginForm.AddField("password", GAME_CONTROLLER_PW);

            var response = await NetworkController.POST("login-form", loginForm);

            if (response.Headers.TryGetValue("Set-Cookie", out var token))
            {
                NetworkController.SetSessionToken(token);
            }
            else
                throw new Exception("No auth token received from login");
        }
    }
}