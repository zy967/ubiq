using Ubiq.XR;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
	public class Chest : MonoBehaviour, IUseable
	{
		private Animator animator;

		private bool open;

		private void Awake()
		{
			animator = GetComponent<Animator>();
		}

		// Start is called before the first frame update
		private void Start()
		{
		}

		// Update is called once per frame
		private void Update()
		{
		}

		public void Use(Hand controller)
		{
			open = !open;
			animator.SetBool("OpenChest", open);
		}

		public void UnUse(Hand controller)
		{
		}
	}
}