﻿using UnityEngine;
using System.Collections;
using Gist;

namespace SimpleFluid {
    [RequireComponent(typeof(Camera))]
    public class FluidEffect : MonoBehaviour {
		public enum RenderMode { Normal = 0, Force, Fluid, AdvectionSource, AdvectedImage }

        public const string FLUIDABLE_KW_SOURCE = "FLUIDABLE_OUTPUT_SOURCE";

        public const string PROP_FLUID_TEX = "_FluidTex";
        public const string PROP_IMAGE_TEX = "_ImageTex";
		public const string PROP_REF_TEX = "_RefTex";
        public const string PROP_DT = "_Dt";

		public TextureEvent OnUpdateAdvectedImageTexture;

		public RenderMode renderMode;
		public Solver solver;

        public Material advectMat;
        public Material lerpMat;
        public Material fluidEffectMat;

        public int lod = 1;

        Camera _attachedCamera;
    	LODRenderTexture _imageTex0;
        LODRenderTexture _imageTex1;
		ManuallyRenderCamera manualCam;
		LODRenderTexture _sourceTex;

		#region Unity
        void Start() {
            _attachedCamera = GetComponent<Camera> ();
            _attachedCamera.depthTextureMode = DepthTextureMode.Depth;
			
            manualCam = new ManuallyRenderCamera (_attachedCamera);
            _imageTex0 = new LODRenderTexture (_attachedCamera, 0, 0, RenderTextureFormat.ARGBFloat);
            _imageTex1 = new LODRenderTexture (_attachedCamera, 0, 0, RenderTextureFormat.ARGBFloat);
            _sourceTex = new LODRenderTexture (_attachedCamera, 0, 24, RenderTextureFormat.ARGB32);

            _imageTex0.AfterCreateTexture += UpdateAfterCreateTexture;
            _imageTex1.AfterCreateTexture += UpdateAfterCreateTexture;

            Prepare ();
        }

        void UpdateAfterCreateTexture (LODRenderTexture obj) {
            obj.Texture.wrapMode = TextureWrapMode.Clamp;
            obj.Texture.filterMode = FilterMode.Bilinear;
            obj.Clear (Color.clear);
        }
        void Update() {
            var dt = solver.DeltaTime;
            Prepare ();
            solver.Solve(dt);
            UpdateImage (dt);

			CaptureAdvectionSource ();
			InjectSourceColorToImage ();
        }
		void OnRenderImage(RenderTexture src, RenderTexture dst) {
			switch (renderMode) {
			case RenderMode.Fluid:
				Graphics.Blit (solver.FluidTex, dst);
				break;
			case RenderMode.Force:
				Graphics.Blit (solver.ForceTex, dst);
				break;
			case RenderMode.AdvectionSource:
                Graphics.Blit (_sourceTex.Texture, dst);
				break;
			case RenderMode.AdvectedImage:
                Graphics.Blit (_imageTex0.Texture, dst);
				break;
			default:
                #if false
				fluidEffectMat.SetTexture (PROP_IMAGE_TEX, _imageTex0);
				Graphics.Blit (src, dst, fluidEffectMat);
                #else
                Graphics.Blit(src, dst);
                #endif
				break;
			}
		}
        void OnDestroy() {
            manualCam.Dispose ();
            if (_imageTex0 != null) {
                _imageTex0.Dispose ();
                _imageTex0 = null;
            }
            if (_imageTex1 != null) {
                _imageTex1.Dispose ();
                _imageTex1 = null;
            }
            if (_sourceTex != null) {
                _sourceTex.Dispose ();
                _sourceTex = null;
            }
        }
		#endregion

        void Prepare () {
            var width = _attachedCamera.pixelWidth;
            var height = _attachedCamera.pixelHeight;
            solver.SetSize (width >> lod, height >> lod);
            _imageTex0.UpdateTexture ();
            _imageTex1.UpdateTexture ();
            _sourceTex.UpdateTexture ();

        }
		void UpdateImage (float dt) {
			solver.SetProperties (advectMat, PROP_FLUID_TEX);
    		advectMat.SetFloat (PROP_DT, dt);
            Graphics.Blit (_imageTex0.Texture, _imageTex1.Texture, advectMat);
    		Solver.Swap (ref _imageTex0, ref _imageTex1);
    	}

		void CaptureAdvectionSource () {
			Shader.EnableKeyword (FLUIDABLE_KW_SOURCE);
            manualCam.Render (_sourceTex.Texture);
			Shader.DisableKeyword (FLUIDABLE_KW_SOURCE);
		}

		void InjectSourceColorToImage () {
            lerpMat.SetTexture (PROP_REF_TEX, _imageTex0.Texture);
            Graphics.Blit (_sourceTex.Texture, _imageTex1.Texture, lerpMat);
            OnUpdateAdvectedImageTexture.Invoke (_imageTex1.Texture);
			Solver.Swap (ref _imageTex0, ref _imageTex1);
		}

    }
}
