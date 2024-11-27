using UnityEngine;

namespace bet_slum.showRunner
{
    public class MockShowRunner: ShowRunner
    {
        protected async override Awaitable Initialize()
        {
            await LoadGame();
            InitializeMatch();
            await Awaitable.WaitForSecondsAsync(3f);
            StartMatch();
        }
    }
}