using TMPro;
using UnityEngine;
using UnityEngine.UI;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class UILabelSetter_IK: MonoBehaviour
{
	public TextMeshProUGUI floatParam1Label;
	public TextMeshProUGUI floatParam2Label;
	public TextMeshProUGUI floatParam3Label;
	public Slider floatParam1Slider;
	public Slider floatParam2Slider;
	public Slider floatParam3Slider;
	public int mode;

/////////////////////////////////////////////////////////////////////////////////

	void Update()
	{
		// Aim
		if (mode == 0)
		{
			floatParam1Label.text = $"Weight: {floatParam1Slider.value:F2}f";
		}
		// Override
		else if (mode == 1)
		{
			floatParam1Label.text = $"Position Weight: {floatParam1Slider.value:F2}f";
			floatParam2Label.text = $"Rotation Weight: {floatParam2Slider.value:F2}f";
		}
		// FABRIK
		else if (mode == 2)
		{
			floatParam1Label.text = $"Ellen Right Hand IK Weight: {floatParam1Slider.value:F2}f";
			floatParam2Label.text = $"Ellen Left Leg IK Weight: {floatParam2Slider.value:F2}f";
			floatParam3Label.text = $"Snake Tail IK Weight: {floatParam3Slider.value:F2}f";
		}
		// FABRIK
		else if (mode == 3)
		{
			floatParam2Label.text = $"Right Leg IK Weight: {floatParam2Slider.value:F2}f";
			floatParam3Label.text = $"Left Leg IK Weight: {floatParam3Slider.value:F2}f";
		}
	}
}
}
