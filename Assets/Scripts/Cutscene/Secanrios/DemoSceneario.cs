using Cutscenes;
using NUnit.Framework;
using UnityEngine.Events;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DemoSceneario : MonoBehaviour
{
    public CutsceneManager CM;
    public List<CutsceneSO> Cutscenes;
    private int currentCutScene = -1;

    // unique to each class, no way around it for the unique things that may need to happen after a cutscene.
    public UnityEvent unityEvent;
    private Villager jarl;
    public GameObject weaponIndicator;
    public Enemy[] enemies;
    private int deadCount;


    private void Start()
    {
        CM = CutsceneManager.Instance;
        // should be played from game manager
        Invoke("TriggerNextCutScene", 0.5f);
    }

    public void TriggerNextCutScene()
    {
        currentCutScene++;
        if(Cutscenes.Count > currentCutScene)
        {
            if (currentCutScene == 0)
            {
                foreach (Enemy enemy in enemies)
                {
                    enemy.OnDeath += () => OnEnemyDied();
                }
                jarl = SettlementManager.Instance.GetCurrentJarl();
                jarl.itemAttachment.onWeaponEquiped += OnEquip;
                CM.OnCutsceneStarted += OnStart0;
                CM.OnCutsceneEnded += OnEnd0;
            }
            CM.PlayCutscene(Cutscenes[currentCutScene]);
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
