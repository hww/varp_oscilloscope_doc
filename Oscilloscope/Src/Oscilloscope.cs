﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using UnityEngine.UI;
using UnityEngine;

namespace VARP.OSC
{
	/// <summary>
	/// Two inputs oscilloscope
	/// </summary>
	public class Oscilloscope : MonoBehaviour
	{
		// =============================================================================================================
		// Fields
		// =============================================================================================================
		
		[Header("Oscilloscope")] 
		public OscSettings oscSettings;
		public OscRenderer oscRenderer; 				//< Waveforms renderer
		public OscLabelManager valueLables;				//< Value labels
		public OscLabelManager timeLables;				//< Time labels
		public OscGuiManager guiManager;
		[Header("Horizontal")] 

		public OscTrigger trigger; 				//< The trigger object 
		[Header("Vetical")] 
		public OscChannel[] oscChannels; 				//< The osciloscope's channels
		[Header("Grid")] 
		public OscGrid grid;   							//< The grid of oscilloscope

		
		private Color bgColorForRenderer;
		private bool isInitialized; 					//< Is this oscilloscope initialized

		// =============================================================================================================
		// Mono Behaviour
		// =============================================================================================================

		void Awake() { Initialize(); }

		void OnEnable() { StartCoroutine(LateFixedUpdateCo()); }

		void OnDisable() { StopAllCoroutines(); }

		IEnumerator LateFixedUpdateCo()
		{
			var waitFixedUpdate = new WaitForFixedUpdate();
			while (true)
			{
				yield return waitFixedUpdate;
				trigger.UpdateTrigger();
			}
		}

		void Update()
		{
			RenderGUI();
		}
		
		// =============================================================================================================
		// Initialization
		// =============================================================================================================

		public void Initialize()
		{
			if (isInitialized)
				return;
			isInitialized = true;
			oscSettings.Initialize(Time.fixedDeltaTime);
			// -- create and render scope grid
			grid.Initialize(oscSettings);

			// -- initialize renderer
			bgColorForRenderer = Color.black;
			oscRenderer.Initialize(oscSettings);
			oscRenderer.Clear(bgColorForRenderer);
			oscRenderer.Apply();
			// -- channels setup --
			oscChannels[0].Initialize(this, OscProbe.Null, OscSettings.BUFFER_SIZE);
			oscChannels[1].Initialize(this, OscProbe.Null, OscSettings.BUFFER_SIZE);
			oscChannels[2].Initialize(this, OscProbe.Null, OscSettings.BUFFER_SIZE);
			oscChannels[3].Initialize(this, OscProbe.Null, OscSettings.BUFFER_SIZE);

			// -- horizontal features --
			oscChannels[0].Initialize(this, OscProbe.Null, OscSettings.BUFFER_SIZE);
			trigger.Initialize(this, oscChannels[1]);
			guiManager.Initialize(this);
		}


		// =============================================================================================================
		// Available Inputs
		// =============================================================================================================

		/// <summary>Call to update channels by probes, and trigger by channel</summary>
		public void UpdateChannelSettings()
		{
			for (var i = 0; i < oscChannels.Length; i++)
				oscChannels[i].OnPlugHandle();
		}
		
		/// <summary>Get channel by name</summary>
		public OscChannel GetChannel(OscChannel.Name channelName)
		{
			return oscChannels[(int) channelName % oscChannels.Length];
		}

		// =============================================================================================================
		// Sampling Rate
		// =============================================================================================================

		/// <summary>
		/// Sample all available channels. Call this method once per frame to
		/// record samples
		/// </summary>
		public void AquireSampe(int dmaWrite)
		{
			var addr = dmaWrite & OscSettings.BUFFER_INDEX_MASK;
			// -- for each input record sample --
			for (var i = 0; i < oscChannels.Length; i++)
			{
				var channel = oscChannels[i];
				var probe = channel.probe;

				if (channel.Decoupling)
				{
					channel[addr] = probe.GetSample(oscSettings.timePerSample);
				}
				else
				{
					channel[addr] = probe.GetSample();
				}
			}
		}

		// =============================================================================================================
		// Renderrer
		// =============================================================================================================

		/// <summary>
		/// Render all scope channels now
		/// </summary>
		public void Render(int smpStart, int smpEnd, float pixStart, float pixPerSample)
		{
			oscRenderer.OnBeforeRenderer();
			oscRenderer.ClearHorizFrame(
				Mathf.RoundToInt(pixStart) , 
					Mathf.RoundToInt(pixStart + ((smpEnd - smpStart) * pixPerSample)),
				bgColorForRenderer); /* skip one pixel */
			for (var i = 0; i < oscChannels.Length; i++)
			{
				var chan = oscChannels[i];
				chan.Render(oscRenderer, smpStart, smpEnd, pixStart, pixPerSample);
			}
			oscRenderer.Apply();
		}

		/// <summary>
		/// update texts and labels
		/// </summary>
		public void RenderGUI()
		{
			for (var i = 0; i < oscChannels.Length; i++)
			{
				var chan = oscChannels[i];
				chan.RenderGUI();
			}
		}
		
	}
}