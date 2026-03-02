using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardUtils : MonoBehaviour
{
    public Button[] numberButtons; // 0-9
    public Button deleteButton;
    public Button enterButton;
    public TMP_InputField targetInputField; // 目标输入框
    public int maxLength = 6;

    private StringBuilder _buffer = new StringBuilder();

    void Awake()
    {
        // 🔥 禁用系统键盘
        DisableSystemKeyboard();

        SetupButtons();
    }

    /// <summary>
    /// 禁用 TMP_InputField 的系统键盘
    /// </summary>
    void DisableSystemKeyboard()
    {
        if (targetInputField != null)
        {
            // 方法1：设置为只读（推荐）
            targetInputField.readOnly = true;

            // 方法2：禁用交互（如果需要完全禁用点击）
            // targetInputField.interactable = false;

            // 方法3：移除默认的 Submit 事件
            targetInputField.onSubmit.RemoveAllListeners();
        }
    }

    void SetupButtons()
    {
        for (int i = 0; i < numberButtons.Length && i <= 9; i++)
        {
            int num = i;
            numberButtons[i].onClick.AddListener(() => AddDigit(num.ToString()));
        }

        deleteButton?.onClick.AddListener(() =>
        {
            if (_buffer.Length > 0)
            {
                _buffer.Length--;
                UpdateTarget();
            }
        });

        // enterButton?.onClick.AddListener(OnEnterPressed);
    }

    void AddDigit(string digit)
    {
        if (_buffer.Length < maxLength)
        {
            _buffer.Append(digit);
            UpdateTarget();
        }
    }

    void UpdateTarget()
    {
        if (targetInputField != null)
        {
            targetInputField.text = _buffer.ToString();
        }
    }

    void OnEnterPressed()
    {
        // 隐藏键盘
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        _buffer.Clear();
        if (targetInputField != null)
        {
            _buffer.Append(targetInputField.text);
        }
    }
}