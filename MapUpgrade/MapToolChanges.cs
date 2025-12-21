// Originally developed by Ardot66
// Modified and maintained by Jettcodey
// Licensed under the MIT License

using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Ardot.Jettcodey.REPO.MapUpgrade;

public class MapToolChanges : MonoBehaviour
{
	public static MapToolChanges Instance;
	public static ConfigEntry<float>
		DefaultSize,
		ZoomRate;

	public static void Init()
	{
		ZoomRate = Plugin.Config.Bind(
			"Map",
			"ZoomSpeed",
			1f,
			new ConfigDescription(
				"Controls the speed that the map is zoomed",
				new AcceptableValueRange<float>(0.1f, 10f)
			)
		);
		DefaultSize = Plugin.Config.Bind(
			"Map",
			"DefaultSize",
			1f,
			new ConfigDescription(
				"The starting size of the map",
				new AcceptableValueRange<float>(0.1f, 10f)
			)
		);

		Plugin.Harmony.Patch(
			AccessTools.Method(typeof(PlayerAvatar), "Awake"),
			postfix: new HarmonyMethod(typeof(MapToolChanges), nameof(PlayerAwakePostfix))
		);
	}

	public static void PlayerAwakePostfix(PlayerAvatar __instance)
	{
		__instance.mapToolController.gameObject.AddComponent<MapToolChanges>();
	}

	public MapToolController MapTool;
	public Camera MapCamera;
	public record struct BackgroundObject(Transform Object, Vector3 OriginalScale);
	public BackgroundObject[] BackgroundObjects = new BackgroundObject[3];

	public const float ZoomScale = 2f;
	public float MaxZoom = 1f;
	public bool MapEnabled = true;

	private static FieldInfo _activeField;
	private float _lastZoom = -1f;

	public void Awake()
	{
		MapTool = GetComponent<MapToolController>();

		if (_activeField == null)
		{
			_activeField = Utils.GetField<MapToolController>("Active");
		}

		int backgroundObjectIndex = 0;
		Utils.ForObjectsInTree(Map.Instance.transform.parent, transform => {
			Camera camera = transform.GetComponent<Camera>();

			if (camera != null)
				MapCamera = camera;

			switch (transform.name)
			{
				case "Fog":
				case "Scanlines":
				case "Background":
					{
						BackgroundObjects[backgroundObjectIndex] = new BackgroundObject(transform, transform.localScale);
						backgroundObjectIndex++;
						break;
					}
				case "Completed":
					return false;
			}

			return true;
		});

		SetZoom(DefaultSize.Value);
	}

	public void Start()
	{
		if (PlayerAvatar.instance != null)
		{
			Plugin.UpdatePlayerMapTool(PlayerAvatar.instance);
		}
	}

	public void Update()
	{
		if (!MapEnabled || !(bool)_activeField.GetValue(MapTool))
			return;

		float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
		if (Mathf.Abs(scroll) > 0.0001f)
		{
			float newSize = Mathf.Clamp(MapCamera.orthographicSize / ZoomScale - scroll * ZoomRate.Value, 0.5f, MaxZoom);
			SetZoom(newSize);
		}
	}

	public void SetMapEnabled(bool enabled)
	{
		MapTool.DisplayMesh.enabled = enabled;
		MapTool.DisplayMesh.transform.parent.GetComponentInChildren<Light>().enabled = enabled;
		MapEnabled = enabled;
	}

	public void SetMaxZoom(float zoom)
	{
		if (Mathf.Abs(zoom - MaxZoom) > 0.1f)
			SetZoom(zoom);

		MaxZoom = zoom;
	}

	public void SetZoom(float zoom)
	{
		if (Mathf.Abs(zoom - _lastZoom) < 0.001f)
			return;

		_lastZoom = zoom;
		MapCamera.orthographicSize = zoom * ZoomScale;

		for (int x = 0; x < BackgroundObjects.Length; x++)
		{
			var bgObject = BackgroundObjects[x];
			Vector3 originalScale = bgObject.OriginalScale;
			bgObject.Object.localScale = new Vector3(originalScale.x * zoom, originalScale.y, originalScale.z * zoom);
		}
	}
}