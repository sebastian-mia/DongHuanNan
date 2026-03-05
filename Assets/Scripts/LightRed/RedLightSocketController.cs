using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RedLightSocketController : MonoBehaviour, IController
{
    public RedLightPosition redLightPosition;
    private XRSocketInteractor socketInteractor;
    private readonly string IEFlashLightTag = "FlashLight";
    private Renderer lightMeshRenderer;
    private readonly string emissionTag = "_EMISSION";
    private AudioManager audioManager;

    void Start()
    {
        socketInteractor = GetComponent<XRSocketInteractor>();
        audioManager = this.GetUtility<AudioManager>();
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(ShowFlashLight);
            socketInteractor.selectExited.AddListener(CloseFlashLight);
        }
    }

    private void ShowFlashLight(SelectEnterEventArgs args)
    {
        audioManager.PlayPutDownRedLightSound();
        lightMeshRenderer = args.interactableObject.transform.GetChild(0).GetComponent<Renderer>();
        StartCoroutine(IEFlashLightTag);
    }

    private void CloseFlashLight(SelectExitEventArgs args)
    {
        audioManager.PlayPutDownRedLightSound();
        this.SendCommand<JudgeRouteCommand>(new JudgeRouteCommand(redLightPosition));
        lightMeshRenderer = args.interactableObject.transform.GetChild(0).GetComponent<Renderer>();
        StopCoroutine(IEFlashLightTag);
        lightMeshRenderer.material.DisableKeyword(emissionTag);
    }

    private IEnumerator FlashLight()
    {
        while (true)
        {
            if (lightMeshRenderer != null)
            {
                lightMeshRenderer.material.EnableKeyword(emissionTag);
                yield return new WaitForSeconds(0.5f);
                lightMeshRenderer.material.DisableKeyword(emissionTag);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}