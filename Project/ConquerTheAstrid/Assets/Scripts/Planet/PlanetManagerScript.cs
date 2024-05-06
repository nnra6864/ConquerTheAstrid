using System.Collections;
using System.Collections.Generic;
using Assets.NnUtils.Scripts;
using Core;
using UnityEngine;

namespace Planet
{
    public class PlanetManagerScript : MonoBehaviour
    {
        public PlanetScript CurrentPlanet;
        private PlanetScript _previousPlanet;
        private List<PlanetScript> _planetScripts;
        [SerializeField] private float _transitionTimeMultiplier;

        private void Awake()
        {
            _planetScripts = new();
            for (int i = 0; i < transform.childCount; i++)
            {
                if (!transform.GetChild(i).TryGetComponent<PlanetScript>(out var ps)) continue;
                _planetScripts.Add(ps);
            }
            CurrentPlanet = _planetScripts[0];
        }
        
        public void SwitchPlanet(PlanetScript ps)
        {
            if (CurrentPlanet == ps) return;
            if (_planetSwitchRoutine != null)
            {
                StopCoroutine(_planetSwitchRoutine);
                _planetSwitchRoutine = null;
            }
            
            CurrentPlanet.SwitchFrom();
            _previousPlanet = CurrentPlanet;
            CurrentPlanet = ps;
            ps.SwitchTo();
            
            GameManager.Instance.AudioManager.PlaySFX(AudioManager.Sounds.PlanetSelect, 0.2f);
            _planetSwitchRoutine = StartCoroutine(PlanetSwitchRoutine(ps));
        }
        
        private Coroutine _planetSwitchRoutine;
        private IEnumerator PlanetSwitchRoutine(PlanetScript ps)
        {
            var cam = GameManager.Instance.MainCam;
            var camScript = GameManager.Instance.CameraScript;
            camScript.enabled = false;
            camScript.UpdateValuesFromCurrentPlanet();
            
            float lerpPos = 0;
            var startPos = cam.transform.position;
            var newCamPos = camScript.RotationCenter + (startPos - _previousPlanet.transform.position);
            var targetPos =
                (camScript.RotationCenter - newCamPos).normalized * camScript._currentZoom + camScript.RotationCenter;
            var posDelta = Vector3.Distance(startPos, targetPos);
            var transitionTime = _transitionTimeMultiplier * (posDelta / 1000f);
            transitionTime = transitionTime < 0.5f ? 0.5f : transitionTime;
            
            while (lerpPos < 1)
            {
                lerpPos += Time.deltaTime / transitionTime;
                lerpPos = Mathf.Clamp01(lerpPos);
                var t = Easings.Ease(lerpPos, Easings.Types.CubicInOut);
                cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            camScript.OnPlanetChanged();
            camScript.enabled = true;
            _planetSwitchRoutine = null;
        }
    }
}