using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shell_Option : Shell_Panel
{
    public GameObject sounds;
    public Button exit_Button;
    public Button save_Button;
    //TODO: ���� ���� ���ҽ� ���ֱ�

    private void Start()
    {
        exit_Button.onClick.AddListener(() => {
            shellGameUI.exit_Panel.gameObject.SetActive(true);
            gameObject.SetActive(false);
        });

        save_Button.onClick.AddListener(() =>
        gameObject.SetActive(false)
        );
    }
}
