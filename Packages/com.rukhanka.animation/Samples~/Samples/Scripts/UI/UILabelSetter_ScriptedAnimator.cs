using TMPro;
using UnityEngine;
using UnityEngine.UI;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class UILabelSetter_ScriptedAnimator: MonoBehaviour
{
	public TextMeshProUGUI singleAnimClipTimeLabel;
	public Slider singleAnimClipTimeSlider;
	public TextMeshProUGUI singleAnimClipWeightLabel;
	public Slider singleAnimClipWeightSlider;
	public TextMeshProUGUI weightLabel;
	public GameObject animation1Selector;
	
	public Button singleAnimBtn;
	public Button animationBlendingBtn;

    public static UILabelSetter_ScriptedAnimator Instance { get; private set; }

/////////////////////////////////////////////////////////////////////////////////

    void Awake()
    {
        Instance = this;
        SetColors(singleAnimBtn, animationBlendingBtn);
    }

/////////////////////////////////////////////////////////////////////////////////

	void Update()
	{
		singleAnimClipTimeLabel.text = $"{singleAnimClipTimeSlider.value:F2}";
		singleAnimClipWeightLabel.text = $"{singleAnimClipWeightSlider.value:F2}";
	}
	
/////////////////////////////////////////////////////////////////////////////////

	public void ManualTimeControlChange(bool on)
	{
		singleAnimClipTimeSlider.interactable = on;
		ScriptedAnimatorSampleConf.Instance.manualTimeControl = on;
	}
	
/////////////////////////////////////////////////////////////////////////////////

	public void Animation0Change(int index)
	{
		ScriptedAnimatorSampleConf.Instance.animationIndices.x = index;
	}
	
/////////////////////////////////////////////////////////////////////////////////

	public void Animation1Change(int index)
	{
		ScriptedAnimatorSampleConf.Instance.animationIndices.y = index;
	}
	
/////////////////////////////////////////////////////////////////////////////////

	void SetColors(Button b0, Button b1)
	{
		var c = Color.cyan;
		var colors = b0.colors;
		colors.normalColor = c;
		colors.highlightedColor = c;
		colors.selectedColor = c;
		b0.colors = colors;
		
		c = Color.white;
		colors = b1.colors;
		colors.normalColor = c;
		colors.highlightedColor = c;
		colors.selectedColor = c;
		b1.colors = colors;
	}

/////////////////////////////////////////////////////////////////////////////////

	public void AnimationBlendingClick()
	{
		ScriptedAnimatorSampleConf.Instance.doBlending = true;
		weightLabel.text = "Blend Factor";
		animation1Selector.SetActive(true);
		SetColors(animationBlendingBtn, singleAnimBtn);	
	}
	
/////////////////////////////////////////////////////////////////////////////////

	public void SingleAnimationClick()
	{
		ScriptedAnimatorSampleConf.Instance.doBlending = false;
		weightLabel.text = "Clip Weight";
		animation1Selector.SetActive(false);
		SetColors(singleAnimBtn, animationBlendingBtn);
	}
}
}
