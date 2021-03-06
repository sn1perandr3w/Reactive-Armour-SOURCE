﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyController : MonoBehaviour
{

	public Transform enemyTarget;
	public Transform target;
	public Transform targetStored;
	public int healthLimit;
	public int health;

	public GameObject explosionSound;

	public string enemyName;

	float rotationalDamp = 60.0f;

	float movementSpeed = 10.0f;
	CharacterController cc;

	public List<GameObject> civilians;

	public GameObject currentHitObject;

	private float currentHitDistance;

	public float sphereRadius;
	public float maxDistance = 25.0f;
	public LayerMask layerMask;

	private Vector3 origin;
	private Vector3 direction;

	public List<Transform> tempWayPoints;

	private Vector3 dir1;
	private Vector3 dir2;
	private Vector3 dir3;
	private Vector3 dir4;

	public bool maneuvering = false;

	public GameObject tempWayPoint;
	public GameObject lastKnownPosWayPoint;
	Animator anim;
	AnimatorStateInfo info;

	//Stunning
	float stunTime = 0.0f;
	//Guarding
	float guardTime = 0.0f;
	public bool guarding = false;

	float pursueBuffer = 0.0f;

	public GameObject[] patrolWaypoints;
	int waypointNum = 0;


	float attackCooldown = 0.0f;

	//Time until returning to patrol
	float timeUntilPatrol = 0;

	public GameObject player;
	float distanceToPlayer;

	int selection;
	float strafeTimer;
	int strafeDir;

	public GameObject projectile;

	public GameObject[] itemDrop;

	void Start ()
	{
		anim = GetComponent<Animator> ();
		cc = GetComponent<CharacterController> ();

			player = GameObject.FindGameObjectWithTag ("player");


		if (target != null && target.transform.tag == "player") {
			anim.SetBool ("pursue", true);
		} else if (patrolWaypoints.Length != 0) {
			target = patrolWaypoints [0].transform;
			anim.SetBool ("patrol", true);
		}

		selection = Random.Range (1, 4);

		if (player.GetComponent<playerController> ().getCombatEffectiveness () < 500) {
			healthLimit = 100;
			health = 100;
			enemyName = "Tyrant mod.B";
		} else 
		if (player.GetComponent<playerController> ().getCombatEffectiveness () >= 500) {
			healthLimit = 200;
			health = 200;
			enemyName = "Tyrant mod.A";
		} else 
		if (player.GetComponent<playerController> ().getCombatEffectiveness () >= 1000) {
				healthLimit = 300;
				health = 300;
				enemyName = "Tyrant mod.EX";
		}

		foreach (GameObject civTarget in GameObject.FindGameObjectsWithTag("destructible")) {
			if (civTarget.GetComponent<civEntity> () != null) {
				civilians.Add (civTarget);
			}
		}

	}
	
	// Update is called once per frame
	void Update ()
	{
		
		distanceToPlayer = Vector3.Distance (transform.position, player.transform.position);

		info = anim.GetCurrentAnimatorStateInfo (0);

		Debug.DrawRay (transform.position, transform.forward * 3.0f, Color.blue);

		guarding = false;


		/*
		if (target != null && target.transform.tag == "player" && info.IsName("CombatCiv")) {
			anim.SetBool ("pursue", true);
			anim.SetBool ("combatCiv", false);
		} else if (patrolWaypoints.Length != 0 && info.IsName("CombatCiv")) {
			target = patrolWaypoints [0].transform;
			anim.SetBool ("patrol", true);
			anim.SetBool ("combatCiv", false);
		}
*/



		if (distanceToPlayer < 100.0f && pursueBuffer <= 0.0f && (info.IsName("Patrolling") || info.IsName("Searching") || info.IsName("CombatCiv"))) {
			
				pursueBuffer = 2.0f;


				
				target = player.transform;

				anim.SetBool ("patrol", false);
				anim.SetBool ("search", false);
			    anim.SetBool ("combatCiv", false);
				anim.SetBool ("combat", false);
				anim.SetBool ("rangedAttackCiv", false);
				anim.SetBool ("pursue", true);


		}

		if (attackCooldown > 0) {
			attackCooldown -= Time.deltaTime;
		}
			

		if (info.IsName ("Pursuing")) {
			//print ("PURSUING");
			CollisionAvoidance ();
			Move ();
		} else if (info.IsName ("Patrolling")) {
			//print ("PATROLLING");
			CollisionAvoidance ();
			CheckForCivTargets ();
			Move ();

		} else if (info.IsName ("Searching")) {
			//print ("SEARCHING");
			LowerPursueBuffer ();
			//CollisionAvoidance ();
			Move();
			Turn ();
		} else if (info.IsName ("Combat")) {
			//print ("COMBATTING");
			combatMove ();
		} else if (info.IsName ("MeleeAttack")) {
			//print ("MELEE ATTACKING");
			meleeAttack ();
		}
		if (info.IsName ("RangedAttack")) {
			//print ("RANGED ATTACKING");
			rangedAttack ();
		}
		if (info.IsName ("Guard")) {
			//print ("GUARDING");
			guard ();
		}
		if (info.IsName ("CombatCiv")) {
			//print ("CombattingCiv");
			Turn();
		}
		if (info.IsName ("Stunned")) {
			//print ("STUNNED");
			stun ();
		}
	}

	//Creates a melee attack using a raycast

	public void meleeAttack ()
	{
		if (attackCooldown <= 0.0f) {
			//Melee hit
			RaycastHit matkHit;

			if (Physics.Raycast (transform.position, transform.forward, out matkHit, 10.0f)) {
				print ("Enemy Hit: " + matkHit.transform.gameObject);
				if (matkHit.transform.gameObject.tag == "player") {
					print ("SHOULD BE LOWERING HEALTH");
					matkHit.transform.gameObject.GetComponent<playerController> ().lowerHealth (40);
				}
			}
		}
		attackCooldown = 4.0f;
		anim.SetBool ("combat", true);
		anim.SetBool ("meleeAttack", false);
		newSelection ();
	}

	//Creates a ranged attack using a raycast.

	public void rangedAttack ()
	{
		if (attackCooldown <= 0.0f) {
			print ("RANGED ATTACK!");
			attackCooldown = 6.0f;
			GameObject g = (GameObject)Instantiate (projectile, transform.position + transform.forward * 3.0f, Quaternion.identity);	
			g.GetComponent<Rigidbody> ().AddForce (transform.forward * 4000);
			Destroy (g, 1);
		}


		if (anim.GetBool ("combat") == false && anim.GetBool ("rangedAttack") == true){
			anim.SetBool ("combat", true);
		anim.SetBool ("rangedAttack", false);
		newSelection ();
		}
		else 
		if(anim.GetBool ("combatCiv") == false && anim.GetBool ("rangedAttackCiv") == true)
		{
				anim.SetBool ("combatCiv", true);
				anim.SetBool ("rangedAttackCiv", false);
		}
	}

	//Avoids obstacles and calls Turn if not doing so.

	void CollisionAvoidance ()
	{
		tempWayPoints.Clear ();
		currentHitDistance = maxDistance;
		origin = transform.position + (transform.forward * 1f);
		direction = transform.forward;
		
		dir1 = ((transform.up * 0.5f) + transform.forward);
		dir2 = ((transform.right * 0.5f) + transform.forward);
		dir3 = ((transform.up * -0.5f) + transform.forward);
		dir4 = ((transform.right * -0.5f) + transform.forward);

		RaycastHit hit;

		if (maneuvering == false) {

			if (Physics.SphereCast (origin, sphereRadius, direction, out hit, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal)) {
				currentHitObject = hit.transform.gameObject;
				currentHitDistance = hit.distance;
				print (hit.transform.gameObject.name);

				if (currentHitObject.tag != "player") {
					//TESTING UP
					if (Physics.SphereCast (origin, sphereRadius, dir1, out hit, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal)) {
						//print ("CAN'T GO UP");
					} else {
						print ("CREATING WAYPOINT UP");
						GameObject WayPointUp = GameObject.Instantiate (tempWayPoint, origin + dir1 * maxDistance, Quaternion.identity);
						tempWayPoints.Add (WayPointUp.transform);
					}
					//TESTING RIGHT
					if (Physics.SphereCast (origin, sphereRadius, dir2, out hit, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal)) {
						//print ("CAN'T GO RIGHT");
					} else {
						//print ("CREATING WAYPOINT RIGHT");
						GameObject WayPointRight = GameObject.Instantiate (tempWayPoint, origin + dir2 * maxDistance, Quaternion.identity);
						tempWayPoints.Add (WayPointRight.transform);
					}
					//TESTING DOWN
					if (Physics.SphereCast (origin, sphereRadius, dir3, out hit, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal)) {
						//print ("CAN'T GO DOWN");
					} else {
						//print ("CREATING WAYPOINT DOWN");
						GameObject WayPointDown = GameObject.Instantiate (tempWayPoint, origin + dir3 * maxDistance, Quaternion.identity);
						tempWayPoints.Add (WayPointDown.transform);
					}
					//TESTING LEFT
					if (Physics.SphereCast (origin, sphereRadius, dir4, out hit, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal)) {
						//print ("CAN'T GO LEFT");
					} else {
						//print ("CREATING WAYPOINT LEFT");
						GameObject WayPointLeft = GameObject.Instantiate (tempWayPoint, origin + dir4 * maxDistance, Quaternion.identity);
						tempWayPoints.Add (WayPointLeft.transform);
					}
			
				}
			}
		}

		if (tempWayPoints.Count == 0) {
			Turn ();
		} else {
			targetStored = target;
			Transform tempWayPointToFollow = tempWayPoints [Random.Range (0, tempWayPoints.Count - 1)].transform;
			target = tempWayPointToFollow;
			maneuvering = true;
		}
	}


	void CheckForCivTargets()
	{
		foreach (GameObject civTarget in civilians) {
			if (civTarget != null) {
				float enemyDistanceToCivTarget = Vector3.Distance (this.transform.position, civTarget.transform.position);
				if (enemyDistanceToCivTarget <= 200.0f) {
					target = civTarget.transform;
					anim.SetBool ("combatCiv", true);
					anim.SetBool ("patrol", false);
				} 
			} else if (civTarget == null) {
				civilians.Remove (civTarget);
			}
		}
	}


	//Advances NPC towards target.

	void Move ()
	{
		cc.Move (transform.forward * movementSpeed * Time.deltaTime);
	}

	//Causes strafing when in combat.

	void combatStrafe ()
	{
		
		//print ("DIRECTION SELECTED! " + strafeDir);
		//print ("STRAFE TIMER: " + strafeTimer);
		if (strafeTimer > 0) {

			if (strafeDir == 1) {
				cc.Move (transform.right * movementSpeed * Time.deltaTime);
			} else if (strafeDir == 2) {
				cc.Move (-transform.right * movementSpeed * Time.deltaTime);
			}
		} else {
			strafeDir = Random.Range (1, 3);
			strafeTimer = Random.Range (1, 4);

		}

		strafeTimer -= Time.deltaTime;
	}

	//Faces NPC towards target, be it player or waypoint. Also used to make decisions based on distance to target.

	void Turn ()
	{
		//print ("Target = " + target.name);

		if (target == null && info.IsName("CombatCiv")) 
		{
			target = patrolWaypoints [waypointNum].transform;
			anim.SetBool ("combatCiv", false);
			anim.SetBool ("patrol", true);
		}


		Vector3 pos = target.position - transform.position;
		Quaternion rotation = Quaternion.LookRotation (pos);

		transform.rotation = Quaternion.Slerp (transform.rotation, rotation, rotationalDamp * Time.deltaTime);

		//print ("TARGET = " + target);
		//print ("TARGET TRANSFORM TAG = " + target.transform.tag);

		float distance = Vector3.Distance (transform.position, target.position);

		if (info.IsName ("Pursuing")) {
			if (target.transform.tag == "player" && distance >= 100.0f) {
				print ("LOST TARGET. SEARCHING LAST KNOWN POSITION");
				GameObject lastKnownPosWP = GameObject.Instantiate (lastKnownPosWayPoint, target.position, Quaternion.identity);
				target = lastKnownPosWP.transform;

				anim.SetBool ("pursue", false);
				anim.SetBool ("search", true);
			} else if (distance < 20 && target.transform.tag == "player") {
				
				anim.SetBool ("pursue", false);
				anim.SetBool ("combat", true);
			}
		}

		if (info.IsName ("CombatCiv")) {

			if(anim.GetBool("pursue") == true)
			{
				anim.SetBool ("combatCiv", false);
			}

			if (distance >= 40.0f) {
				Move ();
			} else
			{
				anim.SetBool ("combatCiv", false);
				anim.SetBool ("rangedAttackCiv", true);
			}
		}

		if (info.IsName ("Searching") && target.tag == "lastKnownWayPoint") {
			if (distance > 3.0f) {
				Move ();
			}

			print ("SEARCHING FOR WAYPOINT. DISTANCE TO WAYPOINT = " + distance);
			if (distance <= 3.0f) {
				timeUntilPatrol += Time.deltaTime;
				print ("REACHED LAST KNOWN POSITION");
				if (timeUntilPatrol >= 10.0f) {
					target = patrolWaypoints [waypointNum].transform;
					distance = Vector3.Distance (transform.position, target.position);

					anim.SetBool ("search", false);
					anim.SetBool ("patrol", true);
					timeUntilPatrol = 0.0f;
				}
			}
		}
			
		if (target.transform.tag == "tempWayPoint") {
				
			if (distance < 3.0f) {
				target = targetStored;
				maneuvering = false;
			}
		} else if (target.transform.tag == "wayPoint") {

			if (distance < 3.0f) {
				if (waypointNum == patrolWaypoints.Length - 1) {
					waypointNum = 0;
				} else {
					waypointNum++;
				}
			}
			target = patrolWaypoints [waypointNum].transform;
		}
	}

	//Prevents from performing actions (UNUSED SO FAR)

	public void stun ()
	{
		stunTime += Time.deltaTime;
		if (stunTime > 1) {
			stunTime = 0;

			anim.SetBool ("stun", false);
			anim.SetBool ("combat", true);
		}
	}

	//Prevents from taking damage

	public void guard ()
	{
		while (guardTime < 2) {
			guardTime += Time.deltaTime;
			guarding = true;
		}

		anim.SetBool ("guard", false);
		anim.SetBool ("combat", true);
		guardTime = 0;
		attackCooldown = 3.0f;
		newSelection ();
	}

	//Moving once in combat range

	public void combatMove ()
	{
		Turn ();

		if (distanceToPlayer > 4.0f && distanceToPlayer < 100.0f) {
			Move ();
		} else if (distanceToPlayer <= 4.0f) {
			combatStrafe ();
		} else {
			anim.SetBool ("combat", false);
			anim.SetBool ("pursue", true);	
		}

		print ("SELECTION = " + selection);
		if (attackCooldown <= 0.0f) {
			if (selection == 1 && distanceToPlayer <= 4.0f) {
				
				anim.SetBool ("combat", false);
				anim.SetBool ("meleeAttack", true);
			} else if (selection == 2) {

				anim.SetBool ("combat", false);
				anim.SetBool ("rangedAttack", true);
			} else if (selection == 3) {
				
				anim.SetBool ("combat", false);
				anim.SetBool ("guard", true);
			}
		}
	}

	//Creates a selection of which action to perform in combat

	public void newSelection ()
	{
		selection = Random.Range (1, 4);
	}

	//Used for not instantly falling back into pursue when in searching if player skims the boundary of the enemy line of sight.

	public void LowerPursueBuffer ()
	{
		pursueBuffer -= Time.deltaTime;
	}

	//Increases health

	public void increaseHealth (int amount)
	{
		if ((health + amount) < healthLimit) {
			health = health + amount;
		} else {
			health = healthLimit;
		}
	}

	//Lowers health

	public void lowerHealth (int amount)
	{
		health = health - amount;
		if (health <= 0) {
			int dropSel = Random.Range(1, 100);
			int itemSel = Random.Range(0,2);

			print ("DROPSEL: " + dropSel + " ITEMSEL: " + itemSel);
			if (itemSel == 0) {
				if (player.GetComponent<playerController> ().getHealth () < 50) {
					dropSel += 40;
				}
			}

			if (itemSel == 1) {
				if (player.GetComponent<playerController> ().getAmmo () < 50) {
					dropSel += 40;
				}
			}

			if(dropSel >= 80)
			{
				GameObject g = (GameObject)Instantiate (itemDrop[itemSel], transform.position, Quaternion.identity);
			}

			GameObject e = (GameObject)Instantiate(explosionSound, transform.position, Quaternion.identity);
			player.GetComponent<playerController> ().increaseExperience (100);
			player.GetComponent<playerController> ().increaseCombatEffectiveness (100);

			Destroy (this.gameObject);
		}

		if (anim.GetBool ("rangedAttackCiv") == false) {

			anim.SetBool ("stun", true);
			anim.SetBool ("guard", false);
			anim.SetBool ("rangedAttack", false);
			anim.SetBool ("meleeAttack", false);
			anim.SetBool ("combat", false);
		}

		print ("Enemy: " + gameObject.name  + " Lowered health by " + amount + " health = " + health);
	}

	//Draws debug lines/spherecasts
	/*
	private void OnDrawGizmosSelected ()
	{
		Gizmos.color = Color.red;
		Debug.DrawLine (origin, origin + direction * maxDistance);
		Gizmos.DrawWireSphere (origin + direction * currentHitDistance, sphereRadius);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere (origin + dir1 * maxDistance, sphereRadius);
		Debug.DrawLine (origin, origin + dir1 * maxDistance);
		Gizmos.DrawWireSphere (origin + dir2 * maxDistance, sphereRadius);
		Debug.DrawLine (origin, origin + dir2 * maxDistance);
		Gizmos.DrawWireSphere (origin + dir3 * maxDistance, sphereRadius);
		Debug.DrawLine (origin, origin + dir3 * maxDistance);
		Gizmos.DrawWireSphere (origin + dir4 * maxDistance, sphereRadius);
		Debug.DrawLine (origin, origin + dir4 * maxDistance);


		
		Debug.DrawLine (origin + dir1 * maxDistance, target.position);
		Debug.DrawLine (origin + dir2 * maxDistance, target.position);
		Debug.DrawLine (origin + dir3 * maxDistance, target.position);
		Debug.DrawLine (origin + dir4 * maxDistance, target.position);
	

		//FIX
		//Gizmos.DrawWireSphere ((origin + dir1 * maxDistance), sphereRadius);
		
	}
	*/

}
