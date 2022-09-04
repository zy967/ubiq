﻿using System.Collections.Generic;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class HexMesh : MonoBehaviour
	{
		public Mesh hexMesh;
		private List<Vector3> vertices;
		private List<int> triangles;

		private void Awake()
		{
			GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
			hexMesh.name = "Hex Mesh";
			vertices = new List<Vector3>();
			triangles = new List<int>();
		}

		public void Triangulate(HexCell c, HexGrid hexGrid)
		{
			hexMesh.Clear();
			vertices.Clear();
			triangles.Clear();

			for (int i = 0; i < 6; i++)
			{
				AddTriangle(Vector3.zero,
					hexGrid.Corners[i],
					hexGrid.Corners[i + 1]);
			}

			hexMesh.vertices = vertices.ToArray();
			hexMesh.triangles = triangles.ToArray();
			hexMesh.RecalculateNormals();
		}

		private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
		{
			int vertexIndex = vertices.Count;
			vertices.Add(v1);
			vertices.Add(v2);
			vertices.Add(v3);
			triangles.Add(vertexIndex);
			triangles.Add(vertexIndex + 1);
			triangles.Add(vertexIndex + 2);
		}
	}
}