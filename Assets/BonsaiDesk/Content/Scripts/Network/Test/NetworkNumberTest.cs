using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using Random = UnityEngine.Random;

public class NetworkNumberTest : NetworkBehaviour
{
    [SyncVar(hook = nameof(NumberHook))] private int _number = 0;

    public TextMeshProUGUI text;

    public TextMeshProUGUI textLocal;

    private void Update()
    {
        textLocal.text = _number.ToString();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _number = 25;
    }

    private void NumberHook(int oldValue, int newValue)
    {
        text.text = newValue.ToString();
    }
    
    public void Button()
    {
        CmdChangeNumber();
    }

    public void Button2()
    {
        ChangeNumber();
    }

    private void ChangeNumber()
    {
        _number = Random.Range(100, 200);
    }

    [Command(ignoreAuthority = true)]
    private void CmdChangeNumber()
    {
        _number = Random.Range(100, 200);
    }
}