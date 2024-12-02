using Google.Cloud.SecretManager.V1;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace bet_slum.showRunner
{
    public class LiveShowRunner: ShowRunner
    {
        // NOTE - Google SecretManager not working here - not sure if a Unity issue (.net version?) - doesn't appear to be an auth problem
        //private const string GameControllerEmailSecretKey = "GAME_CONTROLLER_EMAIL";
        //private const string GameControllerPWSecretKey = "GAME_CONTROLLER_PW";

        // NOTE - these user details are compromised and should only be used for testing
        private const string GAME_CONTROLLER_EMAIL = "0de94eda24d841a09305140b73dae246@net.slum";
        private const string GAME_CONTROLLER_PW = "TnzS$gQLk5NveIen";

        protected async override Awaitable Initialize()
        {
            await LoginToAPI();

            // make sure game state is in sync from last run
            await NetworkController.GET("game", "reset-state");
            await base.Initialize();
        }

        private async Awaitable LoginToAPI()
        {
            Debug.Log("Logging in...");
            var loginForm = new WWWForm();
            loginForm.AddField("email", GAME_CONTROLLER_EMAIL);
            loginForm.AddField("password", GAME_CONTROLLER_PW);

            var response = await NetworkController.POST("account", "game-login", loginForm);

            if (response.Headers.TryGetValue("Set-Cookie", out var token))
            {
                Debug.Log("Login Success!");
                NetworkController.SetSessionToken(token);
            }
            else
                throw new Exception("No auth token received from login");
        }
    }
}