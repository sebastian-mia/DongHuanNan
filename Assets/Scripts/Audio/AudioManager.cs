using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class AudioManager : IUtility
{
    private string buttonHoverPath = $"resources://Voice/Hover";
    private string buttonClickPath = $"resources://Voice/Click";
    private string turnoutBackGroundMusicPath = $"resources://Voice/Turnout";
    private string turnoutMachineWorkingSoundPath = $"resources://Voice/转辙机转动声音";
    private string turnoutMachineEndingSoundPath = $"resources://Voice/转辙机落槽声音";
    private string turnoutMachineCutPowerSoundPath = $"resources://Voice/转辙机断电声音";

    private string gameStartSoundPath = $"resources://Voice/主界面欢迎";
    private string gameStartSoundSecondPath = $"resources://Voice/主界面欢迎2";
    private string waitLongTimeSoundPath = $"resources://Voice/时间过长";
    private string turnoutProjectCompletePerfectSoundPath = $"resources://Voice/完美完成";
    private string turnoutProjectCompleteSoundPath = $"resources://Voice/完成（有错误）";
    private string turnoutProjectRouteFaultSoundPath = $"resources://Voice/转辙机断电声音";
    private string turnoutProjectRedLightFaultSoundPath = $"resources://Voice/转辙机断电声音";
    private string turnoutProjectByeSoundPath = $"resources://Voice/再见";

    //Queue
    private Queue<string> voiceQueue = new Queue<string>();
    private bool isPlaying = false;
    private AudioPlayer currentPlayer = null;

    public void PlayButtonHoverSound()
    {
        PlaySound(buttonHoverPath);
    }

    public void PlayButtonClickSound()
    {
        PlaySound(buttonClickPath);
    }

    public void PlayturnoutBackGroundMusic()
    {
        PlayBackGroundMusic(turnoutBackGroundMusicPath);
    }

    public void PlayturnoutMachineEndingSound()
    {
        PlaySound(turnoutMachineEndingSoundPath);
    }

    public void PlayturnoutMachineWorkingSound()
    {
        AudioKit.SoundFrameCountForIgnoreSameSound = 10;
        PlaySound(turnoutMachineWorkingSoundPath);
    }

    public void PlayturnoutMachineCutPowerSound()
    {
        PlaySound(turnoutMachineCutPowerSoundPath);
    }

    public void PlayVoiceInMainMenuScene()
    {
        AudioKit.PlayVoice(gameStartSoundPath);
    }

    public void PlayVoiceSecondInMainMenuScene()
    {
        AudioKit.PlayVoice(gameStartSoundSecondPath);
    }

    public void PlayVoiceWaitLongTimeSound()
    {
        AudioKit.PlayVoice(waitLongTimeSoundPath);
    }

    public void PlayVoiceInPerfectComplete()
    {
        AudioKit.PlayVoice(turnoutProjectCompletePerfectSoundPath);
    }

    public void PlayVoiceInComplete()
    {
        AudioKit.PlayVoice(turnoutProjectCompleteSoundPath);
    }

    public void PlayVoiceInRouteFault()
    {
        AudioKit.PlayVoice(turnoutProjectRouteFaultSoundPath);
    }

    public void PlayVoiceInRedLightFault()
    {
        AudioKit.PlayVoice(turnoutProjectRedLightFaultSoundPath);
    }

    public void PlayVoiceInBye()
    {
        AudioKit.PlayVoice(turnoutProjectByeSoundPath);
    }

    public void PlayBobVoice(string path)
    {
        AudioKit.PlayVoice(path);
    }


    private void PlayBackGroundMusic(string path)
    {
        AudioKit.PlayMusic(path);
    }

    private void PlayVoice(string path)
    {
        AudioKit.PlayVoice(path);
    }

    private void PlaySound(string path)
    {
        AudioKit.PlaySound(path, playSoundMode: AudioKit.PlaySoundModes.IgnoreSameSoundInSoundFrames);
    }

    /// <summary>
    /// 添加多个语音到队列
    /// </summary>
    public void EnqueueVoice(params string[] voiceNames)
    {
        foreach (var voice in voiceNames)
        {
            voiceQueue.Enqueue(voice);
        }

        if (!isPlaying)
        {
            PlayNext();
        }
    }

    /// <summary>
    /// 清空队列并停止当前播放
    /// </summary>
    public void ClearQueue()
    {
        voiceQueue.Clear();
        isPlaying = false;

        if (currentPlayer != null)
        {
            currentPlayer.Stop();
            currentPlayer = null;
        }
    }

    private void PlayNext()
    {
        if (voiceQueue.Count == 0)
        {
            isPlaying = false;
            currentPlayer = null;
            return;
        }

        isPlaying = true;
        string nextVoice = voiceQueue.Dequeue();

        // 🔥 关键修改：使用 PlaySound 代替 PlayVoice
        // Sound 允许多个同时播放，不会互相中断
        currentPlayer = AudioKit.PlaySound(nextVoice, loop: false,
            callBack: (player) =>
            {
                // 当前语音结束，播放下一段
                PlayNext();
            });
    }

    void OnDestroy()
    {
        ClearQueue();
    }
}