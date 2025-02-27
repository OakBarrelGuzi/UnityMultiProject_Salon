using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Salon.ShellGame
{
    public class ShellGameUI : Panel
    {
        public ShellGameManager shuffleManager { get;private set; }

        public List<Shell_Panel> shell_Panels = new List<Shell_Panel>();
         


        //[Header("Panel Setting")]
        public Shell_Difficult difficult_Panel;
        public Shell_Betting betting_Panel;
        public Shell_Clear clear_Panel;
        public Shell_GameOver gameOver_Panel;
        public Shell_GameInfo gameInfo_Panel;
        public Shell_Option option_Panel;
        public Shell_Exit exit_Panel;
        public Shell_MyGold myGold_Panel;
        public CountStart countStart;
        [Header("Strat Count")]
        [SerializeField]
        private GameObject[] fxObj;
        [SerializeField]
        private float fxDelay = 1;

        public void Initialize(ShellGameManager shuffleManager)
        {
            this.shuffleManager = shuffleManager;

            foreach(var shellpanel in shell_Panels)
            {
                shellpanel.Initialize(this);
                shellpanel.gameObject.SetActive(false);
            }
                
            PanelOpen(difficult_Panel);
            countStart.gameObject.SetActive(true);
        }


        public void PanelOpen(Shell_Panel OpenPanel, Shell_Panel OffPanel)
        {
            OffPanel.gameObject.SetActive(false);
            PanelOpen(OpenPanel);
        }
        public void PanelOpen(Shell_Panel OpenPanel)
        {
            if (OpenPanel is Shell_Betting Betting)
            {
                Betting.gameObject.SetActive(true);
                Betting.bettingSlider.maxValue = shuffleManager.maxBetting[shuffleManager.shellDifficulty];
                Betting.myGoldText.text = shuffleManager.myGold.ToString();
                myGold_Panel.gameObject.SetActive(false);
                return;
            }
            OpenPanel.gameObject.SetActive(true);
            myGold_Panel.gameObject.SetActive(true);
        }


        public IEnumerator PlayCount()
        {
            foreach (GameObject fx in fxObj)
            {
                fx.SetActive(false);
                fx.SetActive(true);
                yield return new WaitForSeconds(fxDelay);
                fx.SetActive(false);
            }
        }
    }
}

