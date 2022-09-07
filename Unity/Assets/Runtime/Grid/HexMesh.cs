using UnityEngine;

namespace Ubiq.Grid
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class HexMesh : MonoBehaviour
	{
		private MeshRenderer meshRenderer;

		private void Awake()
		{
			meshRenderer = GetComponent<MeshRenderer>();
		}

		public void SetVisible(bool isVisible)
		{
			meshRenderer.enabled = isVisible;
		}
	}
}