using bet_slum.Data;
using Google.Cloud.SecretManager.V1;
using Newtonsoft.Json;
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

        // prevent repeated calls while waiting on api
        private bool _roundEnded = false;

        protected async override Awaitable Initialize()
        {
            await LoginToAPI();

            await NetworkController.GET("game", "start-game");
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

        protected async override Awaitable StartBettingPeriod()
        {
            await NetworkController.GET("game", "open-bets");
            await base.StartBettingPeriod();
        }

        protected async override Awaitable EndBettingPeriod()
        {
            await NetworkController.GET("game", "close-bets");
            await base.EndBettingPeriod();
        }

        protected override void StartRound()
        {
            _roundEnded = false;
            base.StartRound();
        }

        public async override Awaitable OnMatchEnd(int winnerID)
        {
            if (_roundEnded) return;
            _roundEnded = true;// prevent repeat calls while we wait for calls

            var bodyForm = new WWWForm();
            bodyForm.AddField("WinnerID", winnerID);
            await NetworkController.POST("game", "payout-bets", bodyForm);

            await base.OnMatchEnd(winnerID);
        }

        public override async Awaitable<GameCompetitionInfo> GetCompetitors()
        {
            var bodyForm = new WWWForm();
            bodyForm.AddField("Count", _matchRunner.MaxCompetitorCount.ToString());
            var competitorResponse = await NetworkController.POST("game", "get-competitors", bodyForm);
            if (competitorResponse.Error != null)
            {
                Debug.LogError($"GETTING COMPETITORS FAILED");
                Debug.LogError(competitorResponse.Error);
                return await base.GetCompetitors();
            }
            return JsonConvert.DeserializeObject<GameCompetitionInfo>(competitorResponse.Text);
        }

        private async void OnDestroy()
        {
            await NetworkController.GET("game", "stop-game");
        }
    }
}