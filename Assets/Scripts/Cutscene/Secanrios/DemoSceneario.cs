using Cutscenes;
using NUnit.Framework;
using UnityEngine.Events;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DemoSceneario : Sceneario
{
    // unique to each class, no way around it for the unique things that may need to happen after a cutscene.
    public UnityEvent unityEvent;
    private Villager jarl;
    public GameObject weaponIndicator;
    public Enemy[] enemies;
    private int deadCount;

    private void Awake()
    {
        instance = this;
    }

    public override void Init()
    {
        base.Init();
        LoadWatchers();
    }

    public void StartNewGame()
    {
        currentCutScene = -1;
        TriggerNextCutScene();
    }

    public void TriggerNextCutScene()
    {
        int nextCutScene = currentCutScene + 1;
        if(Cutscenes.Count > nextCutScene)
        {
            SetWatchers();
            CM.PlayCutscene(Cutscenes[nextCutScene]);
        }
        currentCutScene = nextCutScene;
    }

    public override void SetWatchers()
    {
        if (currentCutScene == -1)
        {
            foreach (Enemy enemy in enemies)
            {
                enemy.OnDeath += () => OnEnemyDied();
                enemy.gameObject.SetActive(true);
            }
            jarl = SettlementManager.Instance.GetCurrentJarl();
            jarl.itemAttachment.onWeaponEquiped += OnEquip;
            CM.OnCutsceneStarted += OnStart0;
            CM.OnCutsceneEnded += OnEnd0;
        }
    }

    public override void LoadWatchers()
    {
        if (currentCutScene == 0)
        {
            foreach (Enemy enemy in enemies)
            {
                enemy.OnDeath += () => OnEnemyDied();
                enemy.gameObject.SetActive(true);
            }
            jarl = SettlementManager.Instance.GetCurrentJarl();
            if (jarl.itemAttachment.weapon != null) 
            {
                weaponIndicator.SetActive(false);
                return;
            }
            jarl.itemAttachment.onWeaponEquiped += OnEquip;
            OnEnd0(null);
        }
    }

    void OnEquip()
    {
        weaponIndicator.SetActive(false);
        jarl.itemAttachment.onWeaponEquiped -= OnEquip;
    }

    public void OnStart0(CutsceneSO _)
    {

    }

    public void OnEnd0(CutsceneSO _)
    {
        unityEvent.Invoke();
        CM.OnCutsceneStarted -= OnStart0;
        CM.OnCutsceneEnded -= OnEnd0;
    }

    private void OnEnemyDied()
    {
        deadCount++;
        if(deadCount >= enemies.Length) TriggerNextCutScene();
    }
}
