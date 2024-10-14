using TMPro;
using UnityEngine;
using UnityEngine.UI;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class UILabelSetter_AnimationCulling: MonoBehaviour
{
	public TextMeshProUGUI floatParam1Label;
	public Slider floatParam1Slider;

    public static UILabelSetter_AnimationCulling Instance { get; private set; }

/////////////////////////////////////////////////////////////////////////////////

    void Awake()
    {
        Instance = this;
    }

/////////////////////////////////////////////////////////////////////////////////

	void Update()
	{
        floatParam1Label.text = $"{floatParam1Slider.value:F2}";
	}
}
}
