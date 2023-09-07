using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Guilds
{
	public class GuildColorPicker : MonoBehaviour
	{
		/// <summary>
		/// Event that gets called by the GuildColorPicker
		/// </summary>
		/// <param name="c">received Color</param>
		private delegate void ColorEvent(Color c);

		private static GuildColorPicker? self;

		/// <returns>
		/// True when the GuildColorPicker is closed
		/// </returns>
		private static bool done = true;

		//onColorChanged event
		private static ColorEvent? onCC;

		//onColorSelected event
		private static ColorEvent? onCS;

		//Color before editing
		private static Color32 originalColor;

		//current Color
		private static Color32 modifiedColor;
		private static HSV modifiedHsv = null!;

		public string chosenColor = "#000000";

		private bool interact;

		// these can only work with the prefab and its children
		public RectTransform positionIndicator = null!;
		public Slider mainComponent = null!;
		public Slider rComponent = null!;
		public Slider gComponent = null!;
		public Slider bComponent = null!;
		public TMP_InputField hexComponent = null!;
		public RawImage colorComponent = null!;

		public Image chosenColorPreview = null!;

		private void Awake()
		{
			self = this;
		}

		public void OnColorButtonClicked()
		{
			// Convert my hex string to a color
			ColorUtility.TryParseHtmlString(chosenColor, out Color color);
			Create(color, SetColor, ColorFinished);
		}

		private void SetColor(Color currentColor)
		{
			//Debug.Log("Choosing " + ColorUtility.ToHtmlStringRGBA(currentColor));
		}

		private void ColorFinished(Color finishedColor)
		{
			chosenColorPreview.color = finishedColor;
			chosenColor = $"#{ColorUtility.ToHtmlStringRGBA(finishedColor)}";
			//Debug.Log("You chose the color " + ColorUtility.ToHtmlStringRGBA(finishedColor));
		}


		/// <summary>
		/// Creates a new GuildColorPicker
		/// </summary>
		/// <param name="original">Color before editing</param>
		/// <param name="onColorChanged">Event that gets called when the color gets modified</param>
		/// <param name="onColorSelected">Event that gets called when one of the buttons done or cancel get pressed</param>
		/// <returns>
		/// False if the instance is already running
		/// </returns>
		private static void Create(Color original, ColorEvent onColorChanged, ColorEvent onColorSelected)
		{
			if (self is null)
			{
				return;
			}

			if (done)
			{
				done = false;
				originalColor = original;
				modifiedColor = original;
				onCC = onColorChanged;
				onCS = onColorSelected;
				self.gameObject.SetActive(true);
				self.RecalculateMenu(true);
				self.hexComponent.placeholder.GetComponent<TextMeshProUGUI>().text = "RRGGBB";
				return;
			}
			Done();
		}

		//called when color is modified, to update other UI components
		private void RecalculateMenu(bool recalculateHSV)
		{
			interact = false;
			if (recalculateHSV)
			{
				modifiedHsv = new HSV(modifiedColor);
			}
			else
			{
				modifiedColor = modifiedHsv.ToColor();
			}

			rComponent.value = modifiedColor.r;
			rComponent.transform.GetChild(3).GetComponent<TMP_InputField>().text = modifiedColor.r.ToString();
			gComponent.value = modifiedColor.g;
			gComponent.transform.GetChild(3).GetComponent<TMP_InputField>().text = modifiedColor.g.ToString();
			bComponent.value = modifiedColor.b;
			bComponent.transform.GetChild(3).GetComponent<TMP_InputField>().text = modifiedColor.b.ToString();

			mainComponent.value = (float)modifiedHsv.H;
			rComponent.transform.GetChild(0).GetComponent<RawImage>().color = new Color32(255, modifiedColor.g, modifiedColor.b, 255);
			rComponent.transform.GetChild(0).GetChild(0).GetComponent<RawImage>().color = new Color32(0, modifiedColor.g, modifiedColor.b, 255);
			gComponent.transform.GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, 255, modifiedColor.b, 255);
			gComponent.transform.GetChild(0).GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, 0, modifiedColor.b, 255);
			bComponent.transform.GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, modifiedColor.g, 255, 255);
			bComponent.transform.GetChild(0).GetChild(0).GetComponent<RawImage>().color = new Color32(modifiedColor.r, modifiedColor.g, 0, 255);
			positionIndicator.parent.GetChild(0).GetComponent<RawImage>().color = new HSV(modifiedHsv.H, 1d, 1d).ToColor();
			positionIndicator.anchorMin = new Vector2((float)modifiedHsv.S, (float)modifiedHsv.V);
			positionIndicator.anchorMax = positionIndicator.anchorMin;
			hexComponent.text = ColorUtility.ToHtmlStringRGB(modifiedColor);
			colorComponent.color = modifiedColor;
			onCC?.Invoke(modifiedColor);
			interact = true;
		}

		//used by EventTrigger to calculate the chosen value in color box
		public void SetChooser()
		{
			Transform parent = positionIndicator.parent;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(parent as RectTransform, Input.mousePosition, GetComponentInParent<Canvas>().worldCamera, out Vector2 localpoint);
			localpoint = Rect.PointToNormalized((parent as RectTransform)!.rect, localpoint);
			if (positionIndicator.anchorMin != localpoint)
			{
				positionIndicator.anchorMin = localpoint;
				positionIndicator.anchorMax = localpoint;
				modifiedHsv.S = localpoint.x;
				modifiedHsv.V = localpoint.y;
				RecalculateMenu(false);
			}
		}


		//gets main Slider value
		public void SetMain(float value)
		{
			if (interact)
			{
				modifiedHsv.H = value;
				RecalculateMenu(false);
			}
		}

		//gets r Slider value
		public void SetR(float value)
		{
			if (interact)
			{
				modifiedColor.r = (byte)value;
				RecalculateMenu(true);
			}
		}

		//gets r TMP_InputField value
		public void SetR(string value)
		{
			if (interact)
			{
				modifiedColor.r = (byte)Mathf.Clamp(int.Parse(value), 0, 255);
				RecalculateMenu(true);
			}
		}

		//gets g Slider value
		public void SetG(float value)
		{
			if (interact)
			{
				modifiedColor.g = (byte)value;
				RecalculateMenu(true);
			}
		}

		//gets g TMP_InputField value
		public void SetG(string value)
		{
			if (interact)
			{
				modifiedColor.g = (byte)Mathf.Clamp(int.Parse(value), 0, 255);
				RecalculateMenu(true);
			}
		}

		//gets b Slider value
		public void SetB(float value)
		{
			if (interact)
			{
				modifiedColor.b = (byte)value;
				RecalculateMenu(true);
			}
		}

		//gets b TMP_InputField value
		public void SetB(string value)
		{
			if (interact)
			{
				modifiedColor.b = (byte)Mathf.Clamp(int.Parse(value), 0, 255);
				RecalculateMenu(true);
			}
		}

		//gets hexa TMP_InputField value
		public void SetHexa(string value)
		{
			if (interact)
			{
				if (ColorUtility.TryParseHtmlString("#" + value, out Color c))
				{
					c.a = 1;
					modifiedColor = c;
					RecalculateMenu(true);
				}
				else
				{
					hexComponent.text = ColorUtility.ToHtmlStringRGB(modifiedColor);
				}
			}
		}

		//cancel button call
		public void CCancel()
		{
			Cancel();
		}

		/// <summary>
		/// Manually cancel the GuildColorPicker and recover the default value
		/// </summary>
		private static void Cancel()
		{
			modifiedColor = originalColor;
			Done();
		}

		//done button call
		public void CDone()
		{
			Done();
		}

		/// <summary>
		/// Manually close the GuildColorPicker and apply the selected color
		/// </summary>
		private static void Done()
		{
			done = true;
			onCC?.Invoke(modifiedColor);
			onCS?.Invoke(modifiedColor);
			self!.transform.gameObject.SetActive(false);
		}

		//HSV helper class
		private sealed class HSV
		{
			public double H = 0, S = 1, V = 1;
			private const byte A = 255;

			public HSV()
			{
			}

			public HSV(double h, double s, double v)
			{
				H = h;
				S = s;
				V = v;
			}

			public HSV(Color color)
			{
				float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
				float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));

				float hue = (float)H;
				if (min != max)
				{
					if (max == color.r)
					{
						hue = (color.g - color.b) / (max - min);
					}
					else if (max == color.g)
					{
						hue = 2f + (color.b - color.r) / (max - min);
					}
					else
					{
						hue = 4f + (color.r - color.g) / (max - min);
					}

					hue *= 60;
					if (hue < 0) hue += 360;
				}

				H = hue;
				S = (max == 0) ? 0 : 1d - ((double)min / max);
				V = max;
			}

			public Color32 ToColor()
			{
				int hi = Convert.ToInt32(Math.Floor(H / 60)) % 6;
				double f = H / 60 - Math.Floor(H / 60);

				double value = V * 255;
				byte v = (byte)Convert.ToInt32(value);
				byte p = (byte)Convert.ToInt32(value * (1 - S));
				byte q = (byte)Convert.ToInt32(value * (1 - f * S));
				byte t = (byte)Convert.ToInt32(value * (1 - (1 - f) * S));

				switch (hi)
				{
					case 0:
					{
						return new Color32(v, t, p, A);
					}
					case 1:
					{
						return new Color32(q, v, p, A);
					}
					case 2:
					{
						return new Color32(p, v, t, A);
					}
					case 3:
					{
						return new Color32(p, q, v, A);
					}
					case 4:
					{
						return new Color32(t, p, v, A);
					}
					case 5:
					{
						return new Color32(v, p, q, A);
					}
					default:
					{
						return new Color32();
					}
				}
			}
		}
	}
}
