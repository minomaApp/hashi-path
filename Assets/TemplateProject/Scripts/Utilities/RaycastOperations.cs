using System;
using System.Collections.Generic;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{


	public class RaycastOperations
	{
#region Variables


		private Camera _mainCamera;

		private Ray _cacheRay;
		private RaycastHit _cacheHit;
		private RaycastHit[] _cacheHitArray;


#endregion Variables


#region Functions


		public RaycastOperations(Camera mainCamera)
		{
			_mainCamera = mainCamera;

			_cacheHitArray = new RaycastHit[10];
		}


		public T GetObjectOfTypeWithinAll<T>(Vector3 screenPosition)
		{
			T requiredObject = default;

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity);

			for (int i = 0; i < hitArray.Length; i++)
			{
				bool got = hitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;
				break;
			}

			return requiredObject;
		}


		public T GetObjectOfTypeWithinAllNonAlloc<T>(Vector3 screenPosition)
		{
			T requiredObject = default;

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);
			Array.Clear(_cacheHitArray, 0, _cacheHitArray.Length);

			int hitCount = Physics.RaycastNonAlloc(_cacheRay, _cacheHitArray, Mathf.Infinity);
			for (int i = 0; i < hitCount; i++)
			{
				bool got = _cacheHitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;
				break;
			}

			return requiredObject;
		}


		public T GetObjectOfTypeWithinAll<T>(int layer, Vector3 screenPosition)
		{
			T requiredObject = default;

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);
			var results = new RaycastHit[10];
			Physics.RaycastNonAlloc(_cacheRay, results, Mathf.Infinity, layer);

			if (results.Length > 0)
			{
				for (int i = 0; i < results.Length; i++)
				{
					bool got = results[i].collider.TryGetComponent(out requiredObject);
					if (!got) continue;
					break;
				}
			}

			return requiredObject;
		}


		public T GetObjectOfTypeWithinAllNonAlloc<T>(int layer, Vector3 screenPosition)
		{
			T requiredObject = default;

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);
			Array.Clear(_cacheHitArray, 0, _cacheHitArray.Length);

			int hitCount = Physics.RaycastNonAlloc(_cacheRay, _cacheHitArray, Mathf.Infinity, layer);

			for (int i = 0; i < hitCount; i++)
			{
				bool got = _cacheHitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;
				break;
			}

			return requiredObject;
		}


		public Vector3? GetFingerPointOnGridPlane(Vector2 screenPosition)
		{
			Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

			Plane gridPlane = new Plane(Vector3.up, Vector3.zero);

			if (gridPlane.Raycast(ray, out float enter))
			{
				return ray.GetPoint(enter);
			}

			return null;
		}


		public T GetObjectOfType<T>(Vector3 screenPosition)
		{
			T requiredObject = default;
			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);

			bool hit = Physics.Raycast(_cacheRay, out RaycastHit raycastHit, Mathf.Infinity);
			if (!hit) return requiredObject;

			requiredObject = raycastHit.collider.GetComponent<T>();
			return requiredObject;
		}


		public T GetObjectOfType<T>(int layer, Vector3 screenPosition)
		{
			T requiredObject = default;
			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);

			bool hit = Physics.Raycast(_cacheRay, out _cacheHit, Mathf.Infinity, layer);
			if (hit)
				_cacheHit.collider.TryGetComponent(out requiredObject);

			return requiredObject;
		}


		public T GetObjectOfTypeInChildren<T>(int layer, Vector3 screenPosition)
		{
			T requiredObject = default;
			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);

			bool hit = Physics.Raycast(_cacheRay, out RaycastHit raycastHit, Mathf.Infinity, layer);
			if (hit)
				requiredObject = raycastHit.collider.GetComponentInChildren<T>();

			return requiredObject;
		}


		public T GetObjectOfTypeInParent<T>(int layer, Vector3 screenPosition)
		{
			T requiredObject = default;
			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);

			bool hit = Physics.Raycast(_cacheRay, out RaycastHit raycastHit, Mathf.Infinity, layer);
			if (hit)
				requiredObject = raycastHit.collider.GetComponentInParent<T>();

			return requiredObject;
		}


		public T GetObjectOfTypeByRaycastingWithDirection<T>(GameObject gameObject, Vector3 direction)
		{
			T requiredObject = default;

			_cacheRay = new Ray(gameObject.transform.position, direction);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity);

			for (int i = 0; i < hitArray.Length; i++)
			{
				bool got = hitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;

				requiredObject = hitArray[i].collider.GetComponent<T>();
				break;
			}

			return requiredObject;
		}


		public T GetObjectOfTypeByRaycastingWithDirection<T>(int layer, GameObject gameObject, Vector3 direction)
		{
			T requiredObject = default;
			_cacheRay = new Ray(gameObject.transform.position, direction);

			bool got = Physics.Raycast(_cacheRay, out _cacheHit, Mathf.Infinity, layer);
			if (got)
				_cacheHit.collider.TryGetComponent(out requiredObject);

			return requiredObject;
		}


		public T GetObjectOfTypeByRaycastingWithDirectionParent<T>(GameObject gameObject, Vector3 direction)
		{
			T requiredObject = default;
			_cacheRay = new Ray(gameObject.transform.position, direction);

			bool hit = Physics.Raycast(_cacheRay, out _cacheHit, Mathf.Infinity);
			if (hit && _cacheHit.collider.transform.parent != null)
				_cacheHit.collider.transform.parent.TryGetComponent(out requiredObject);

			return requiredObject;
		}


		public T GetObjectOfTypeByRaycastingWithDirectionWithinAll<T>(int layer, GameObject gameObject,
			Vector3 direction)
		{
			T requiredObject = default;

			_cacheRay = new Ray(gameObject.transform.position, direction);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity, layer);

			for (int i = 0; i < hitArray.Length; i++)
			{
				bool got = hitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;

				return requiredObject;
			}

			return requiredObject;
		}


		public T GetObjectOfTypeByRaycastingWithDirectionWithinAll<T>(GameObject gameObject, Vector3 direction)
		{
			T requiredObject = default;

			_cacheRay = new Ray(gameObject.transform.position, direction);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity);

			for (int i = 0; i < hitArray.Length; i++)
			{
				bool got = hitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;

				return requiredObject;
			}

			return requiredObject;
		}


		public Vector3 GetHitPoint(Vector3 screenPosition)
		{
			Vector3 hitPoint = Vector3.zero;
			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);

			bool didHit = Physics.Raycast(_cacheRay, out RaycastHit hit, Mathf.Infinity);
			if (didHit)
				hitPoint = hit.point;

			return hitPoint;
		}


		public Vector3 GetHitPoint<T>(Vector3 screenPosition)
		{
			T reqiuredObject = default;
			Vector3 hitPoint = Vector3.zero;

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity);

			for (int i = 0; i < hitArray.Length; i++)
			{
				bool got = hitArray[i].collider.TryGetComponent(out reqiuredObject);
				if (!got) continue;

				hitPoint = hitArray[i].point;
			}

			return hitPoint;
		}


		public Vector3 GetHitPointByDirectionFromPosition(Vector3 center, Vector3 direction)
		{
			Vector3 hitPoint = Vector3.zero;
			_cacheRay = new Ray(center, direction);

			bool didHit = Physics.Raycast(_cacheRay, out RaycastHit hit, Mathf.Infinity);
			if (didHit)
				hitPoint = hit.point;

			return hitPoint;
		}


		public Vector3 GetSurfaceNormal(Vector3 screenPosition)
		{
			Vector3 normal = Vector3.zero;

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);

			bool didHit = Physics.Raycast(_cacheRay, out RaycastHit hit, Mathf.Infinity);
			if (didHit)
				normal = hit.normal;

			return normal;
		}


		public Vector3 GetSurfaceNormal<T>(Vector3 screenPosition)
		{
			Vector3 normal = Vector3.zero;

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity);

			for (int i = 0; i < hitArray.Length; i++)
			{
				if (hitArray[i].collider.GetComponent<T>() != null && hitArray[i].collider)
					normal = hitArray[i].normal;
			}

			return normal;
		}


		public Vector3 GetRaycastPlanePoint(Vector3 screenPosition, Vector3 inNormal, Vector3 inPosition)
		{
			Plane plane = new Plane(inNormal, inPosition);
			Vector3 hitPoint = Vector3.zero;

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);

			bool didHit = plane.Raycast(_cacheRay, out float distance);
			if (didHit)
				hitPoint = _cacheRay.GetPoint(distance);

			return hitPoint;
		}


		public List<T> GetObjectsOfType<T>(int layer, Vector3 screenPosition)
		{
			T requiredObject = default;
			List<T> requiredObjectList = new List<T>();

			_cacheRay = _mainCamera.ScreenPointToRay(screenPosition);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity, layer);

			for (int i = 0; i < hitArray.Length; i++)
			{
				bool got = hitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;

				requiredObjectList.Add(requiredObject);
			}

			return requiredObjectList;
		}


		public T GetObjectOfTypeFromTransform<T>(int layer, Vector3 offset, Transform transform, Vector3 direction)
		{
			T requiredObject = default;

			_cacheRay = new Ray(transform.position + offset, direction);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity, layer);

			for (int i = 0; i < hitArray.Length; i++)
			{
				bool got = hitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;
				break;
			}

			return requiredObject;
		}


		public List<T> GetObjectsOfTypeFromTransform<T>(int layer, Vector3 offset, Transform transform,
			Vector3 direction)
		{
			T requiredObject = default;
			List<T> requiredObjectList = new List<T>();

			_cacheRay = new Ray(transform.position + offset, direction);
			RaycastHit[] hitArray = Physics.RaycastAll(_cacheRay, Mathf.Infinity, layer);

			for (int i = 0; i < _cacheHitArray.Length; i++)
			{
				bool got = hitArray[i].collider.TryGetComponent(out requiredObject);
				if (!got) continue;

				requiredObjectList.Add(requiredObject);
			}

			return requiredObjectList;
		}


#endregion Functions
	}


}