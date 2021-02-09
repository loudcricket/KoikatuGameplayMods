﻿using System;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using Illusion.Component;
using KKAPI.Utilities;
using Manager;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KK_LewdCrestX
{
    internal static class ActionIconHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionMap), "Reserve")]
        private static void OnMapChangedHook(ActionMap __instance)
        {
            if (__instance.mapRoot == null || __instance.isMapLoading) return;

            if (__instance.no == 22)
            {
                try
                {
                    SpawnCrestActionPoint();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                }
            }
        }

        private static void SpawnCrestActionPoint()
        {
            LewdCrestXPlugin.Logger.LogDebug("Spawning crest action point");

            if (_iconOff == null)
            {
                _iconOff = (LewdCrestXPlugin.Bundle.LoadAsset<Texture2D>("action_icon_crest_off") ??
                            throw new Exception("asset not found - action_icon_crest_off")).ToSprite();
                Object.DontDestroyOnLoad(_iconOff);
            }

            if (_iconOn == null)
            {
                _iconOn = (LewdCrestXPlugin.Bundle.LoadAsset<Texture2D>("action_icon_crest_on") ??
                           throw new Exception("asset not found - action_icon_crest_on")).ToSprite();
                Object.DontDestroyOnLoad(_iconOn);
            }

            var inst = CommonLib.LoadAsset<GameObject>("map/playeractionpoint/00.unity3d", "PlayerActionPoint_05", true);
            var parent = GameObject.Find("Map/ActionPoints");
            inst.transform.SetParent(parent.transform, true);

            var pap = inst.GetComponentInChildren<PlayerActionPoint>();
            var iconRootObject = pap.gameObject;
            var iconRootTransform = pap.transform;
            Object.DestroyImmediate(pap, false);

            // position above the small table
            iconRootTransform.position = new Vector3(-3.1f, -0.4f, 1.85f);

            if (iconRootObject.GetComponent<ObservableUpdateTrigger>())
                Console.WriteLine("was spawned -=--------------");

            var evt = iconRootObject.AddComponent<TriggerEnterExitEvent>();
            var animator = iconRootObject.GetComponentInChildren<Animator>();
            var rendererIcon = iconRootObject.GetComponentInChildren<SpriteRenderer>();
            rendererIcon.sprite = _iconOff;
            var playerInRange = false;
            evt.onTriggerEnter += c =>
            {
                if (!c.CompareTag("Player")) return;
                playerInRange = true;
                animator.Play("icon_action");
                rendererIcon.sprite = _iconOn;
                c.GetComponent<Player>().actionPointList.Add(evt);
            };
            evt.onTriggerExit += c =>
            {
                if (!c.CompareTag("Player")) return;
                playerInRange = false;
                animator.Play("icon_stop");
                rendererIcon.sprite = _iconOff;
                c.GetComponent<Player>().actionPointList.Remove(evt);
            };

            var player = Singleton<Game>.Instance.actScene.Player;
            evt.UpdateAsObservable()
                .Where(_ => playerInRange && ActionInput.isAction && !player.isActionNow)
                .Subscribe(_ => ClubInterface.ShowWindow = true)
                .AddTo(evt);

            evt.OnGUIAsObservable()
                .Subscribe(ClubInterface.ClubInterfaceOnGui)
                .AddTo(evt);
        }

        private static Sprite _iconOff, _iconOn;
    }
}