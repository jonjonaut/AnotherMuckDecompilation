using System;
using UnityEngine;

public class ProjectileAttackNoGravity : MonoBehaviour
{

	private void SpawnProjectile()
	{
		Vector3 position = this.spawnPos.position;
		Vector3 normalized = (this.mob.transform.position + Vector3.up * 1f).normalized;
		float force = this.projectile.bowComponent.projectileSpeed * this.attackForce;
		int id = this.projectile.id;
		int id2 = 1;
		Quaternion ObjectRotation = this.mob.transform.rotation;
		ServerSend.MobSpawnProjectile(position, normalized, force, id, id2);
		ProjectileController.Instance.SpawnMobProjectile(position, normalized, force, id, id2);
	}

	public void SpawnPredictedWarningAttack()
	{
		Rigidbody component = this.mob.transform.GetComponent<Rigidbody>();
		Vector3 position = this.mob.transform.position;
		Vector3 a = Vector3.zero;
		if (component)
		{
			a = VectorExtensions.XZVector(component.velocity);
		}
		float timeToImpact = this.warningAttack.bowComponent.timeToImpact;
		Vector3 vector2;
		Vector3 vector = (vector2 = position + a * timeToImpact) - vector2;
		float force = 0f;
		int id = this.warningAttack.id;
		int id2 = 1;
		ServerSend.MobSpawnProjectile(vector2, vector, force, id, id2);
		ProjectileController.Instance.SpawnMobProjectile(vector2, vector, force, id, id2);
	}

	public void SpawnProjectilePredictionTrajectory()
	{
		Vector3 position = this.mob.transform.position;
		Rigidbody component = this.projectile.prefab.GetComponent<Rigidbody>();
		Vector3 vector = this.findLaunchVelocity(PlayerInput.Instance.playerCam.transform.localRotation.eulerAngles, this.spawnPos.gameObject);
		float mass = component.mass;
		float fixedDeltaTime = Time.fixedDeltaTime;
		float magnitude = vector.magnitude;
		Vector3 position2 = this.spawnPos.position;
		Vector3 normalized = vector.normalized;
		float force = mass * (magnitude / fixedDeltaTime);
		int id = this.projectile.id;
		int id2 = 1;
		ServerSend.MobSpawnProjectile(position2, normalized, force, id, id2);
		ProjectileController.Instance.SpawnMobProjectile(position2, normalized, force, id, id2);
	}

	public void SpawnProjectilePredictNextPosition()
	{
		Rigidbody component = this.mob.transform.GetComponent<Rigidbody>();
		Vector3 position = this.mob.transform.position;
		Vector3 a = Vector3.zero;
		if (component)
		{
			a = VectorExtensions.XZVector(component.velocity);
		}
		float projectileSpeed = this.predictionProjectile.bowComponent.projectileSpeed;
		Vector3 position2 = this.predictionPos.position;
		float d = Vector3.Distance(position, position2) / projectileSpeed;
		Vector3 a2 = position + a * d;
		Vector3 position3 = this.predictionPos.position;
		Vector3 vector = a2 - position3;
		int id = this.predictionProjectile.id;
		int id2 = 1;
		float force = 0f;
		ServerSend.MobSpawnProjectile(position3, vector, force, id, id2);
		ProjectileController.Instance.SpawnMobProjectile(position3, vector, force, id, id2);
	}

	private Vector3 findLaunchVelocity(Vector3 targetPosition, GameObject newProjectile)
	{
		if (this.useLowestLaunchAngle)
		{
			Vector3 normalized = (targetPosition - newProjectile.transform.position).normalized;
			if (normalized.x > 1f)
			{
				normalized.x = 1f;
			}
			if (normalized.y > 1f)
			{
				normalized.y = 1f;
			}
			if (normalized.z > 1f)
			{
				normalized.z = 1f;
			}
			MonoBehaviour.print("y component: " + normalized.y);
			float num = Mathf.Asin(normalized.y);
			this.launchAngle = num + this.mob.transform.localEulerAngles.x;
			MonoBehaviour.print("launch angle: " + this.launchAngle);
		}
		Vector3 a = new Vector3(this.spawnPos.position.x, this.spawnPos.position.y, this.spawnPos.position.z);
		Vector3 vector = new Vector3(targetPosition.x, this.spawnPos.position.y, targetPosition.z);
		float num2 = Vector3.Distance(a, vector);
		newProjectile.transform.LookAt(vector * -180f);
		float y = Physics.gravity.y;
		this.launchAngle = this.mob.transform.localEulerAngles.x;
		float num3 = Mathf.Tan(this.launchAngle * 0.017453292f);
		float num4 = targetPosition.y - this.spawnPos.position.y;
		float num5 = Mathf.Sqrt(y * num2 * num2 / (2f * (num4 - num2 * num3)));
		float y2 = num3 * num5;
		Vector3 direction = new Vector3(0f, y2, num5);
		Vector3 vector2 = newProjectile.transform.TransformDirection(direction);
		if (float.IsNaN(vector2.x))
		{
			vector2 = (targetPosition - newProjectile.transform.position).normalized;
		}
		return vector2;
	}

	private void Update()
	{
		Rigidbody rb = GetComponent<Rigidbody>();
		PlayerRotationX = rb.rotation.x;
		PlayerRotationY = rb.rotation.y;

		if (Input.GetKeyDown(KeyCode.K)) {
			SpawnProjectilePredictionTrajectory();
		}
	}
	public static ProjectileAttackNoGravity Instance;

	public InventoryItem projectile;

	public InventoryItem predictionProjectile;

	public InventoryItem warningAttack;

	public Transform spawnPos;

	public Transform predictionPos;

	public float attackForce = 1000f;

	private float launchAngle;

	public bool useLowestLaunchAngle;

	public static float PlayerRotationX;

	public static float PlayerRotationY;

	public Vector3 angularVel;

	[SerializeField] private GameObject mob;

	 public GameObject PlayerRotationCam;

}
