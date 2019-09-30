﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SocialPoint.Art.LightingProfiles
{
    [AddComponentMenu("Rendering/Lighting Layer", -10)]
    [DisallowMultipleComponent]
    public class LightingLayer : MonoBehaviour
    {
        #region VARIABLES
        public static LightingLayer Instance;

        public bool blendAllSettings = true;
        public bool switchSkybox = true;
        public bool useEnvLighting = true;
        public bool useEnvReflection = true;
        public bool useMixedLighting = true;
        public bool useFog = true;
        public bool useHalo = true;
        public int frameSkip = 0;

        private LightingProfile initialProfile;
        private LightingProfile temporalProfile;
        private LightingProfile currentProfile;
        private LightingProfile desireProfile;
        public string desiredProfileName;
        public string currentProfileName;

        public float blend = 0;
        
        public bool showDebugLines = false;

        public List<LightingVolume> volumes;

        private float blendTime = 0;
        private AnimationCurve curve;
        private float counter = 0;
        private bool hasToFade = false;

        #endregion

        private void Awake()
        {
            Instance = this;

            CreateInitProfile();

            currentProfile = ScriptableObject.CreateInstance<LightingProfile>();
            temporalProfile = ScriptableObject.CreateInstance<LightingProfile>();
            currentProfile.CopyFromCurrentScene();

            if (blendAllSettings)
                switchSkybox = useEnvLighting = useEnvReflection = useMixedLighting = useFog = useHalo = true;
        }

        internal void CreateInitProfile()
        {
            Debug.Log("Copying init profile settings");
            initialProfile = ScriptableObject.CreateInstance<LightingProfile>();
            initialProfile.CopyFromCurrentScene();
        }

        void Update()
        {
            FadeGlobalSettings();

            //if (Time.frameCount % (frameSkip + 1) != 0)
            //    return;

            //LightingBlendingManager.instance.UpdateLightingSettings(transform.position, switchSkybox, useEnvLighting, useEnvReflection, useMixedLighting, useFog, useHalo, showDebugLines);
            //blend = LightingBlendingManager.instance.GetBlendValue();
        }

        private void FadeGlobalSettings()
        {
            if (!hasToFade) return;

            counter += Time.deltaTime / blendTime;

            temporalProfile.Lerp(currentProfile, desireProfile, curve.Evaluate(counter), switchSkybox, useEnvLighting, useEnvReflection, useMixedLighting, useFog, useHalo);
            temporalProfile.Apply();

            if (counter > 1)
            {
                counter = 0;
                hasToFade = false;
                currentProfile.CopyFromCurrentScene();
                currentProfileName = desireProfile.name;
                desireProfile = null;
                desiredProfileName = "---";
            }
        }

        private void OnDestroy()
        {
            Debug.Log("Settings initial lighting profile");
            initialProfile.Apply();
        }

        internal void Register(LightingVolume volume)
        {
            Debug.Log("<color=green>ADDING</color> New volume of type: " + (volume.isGlobal ? "GLOBAL" : "BLEND"));
            volumes.Add(volume);
            SetDirty(volume.isGlobal);
        }

        internal void Unregister(LightingVolume volume)
        {
            Debug.Log("<color=red>REMOVING</color> lighting volume of type: " + (volume.isGlobal ? "GLOBAL" : "BLEND"));
            volumes.Remove(volume);
            SetDirty(volume.isGlobal);
        }

        private void SetDirty(bool isGlobal)
        {
            if (isGlobal) GetHigherGlobalProfile();
        }


        public void GetHigherGlobalProfile()
        {
            if (ListOfVolumesIsEmpty()) return;

            List<LightingVolume> globalVolumes = new List<LightingVolume>();

            for (int i = 0; i < volumes.Count; i++)
            {
                if (volumes[i].isGlobal && volumes[i].profile != null)
                    globalVolumes.Add(volumes[i]);
            }

            if (globalVolumes.Count > 0)
            {
                LightingVolume v = globalVolumes.OrderBy(p => p.priority).Last();

                desireProfile = v.profile;

                //if (desireProfile == v.profile) return;

                //temporalProfile = currentProfile;
                desiredProfileName = v.profile.name;
                blendTime = v.timeToBlend;
                curve = v.timeCurve;
                hasToFade = true;
            }
            else
            {
                Debug.LogWarning("No global volumes found in the scene.");
                hasToFade = false;
            }
        }

        /// <summary>
        /// Return a warning if there are no lighting volumes in the scene
        /// </summary>
        /// <returns></returns>
        private bool ListOfVolumesIsEmpty()
        {
            if (volumes.Count == 0)
            {
                Debug.LogWarning("There are no volume lightings in the scene.");
                return true;
            }

            return false;
        }
    }
}