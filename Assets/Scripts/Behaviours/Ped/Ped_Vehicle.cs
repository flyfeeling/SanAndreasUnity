﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SanAndreasUnity.Behaviours.Vehicles;
using SanAndreasUnity.Behaviours.World;
using SanAndreasUnity.Importing.Animation;
using System.Linq;

namespace SanAndreasUnity.Behaviours
{
	
	public partial class Ped : MonoBehaviour {

		[SerializeField] private float m_enterVehicleRadius = 2.0f;
		public float EnterVehicleRadius { get { return m_enterVehicleRadius; } set { m_enterVehicleRadius = value; } }

		public Vehicle CurrentVehicle { get; private set; }
		public bool IsInVehicle { get { return CurrentVehicle != null; } }
		public bool IsInVehicleSeat { get; private set; }
		public bool IsDrivingVehicle { get { return this.IsInVehicleSeat && this.CurrentVehicleSeat.IsDriver && this.IsInVehicle; } }
		public Vehicle.Seat CurrentVehicleSeat { get; private set; }
		public Vehicle.SeatAlignment CurrentVehicleSeatAlignment { get { return CurrentVehicleSeat.Alignment; } }



		public void EnterVehicle(Vehicle vehicle, Vehicle.SeatAlignment seatAlignment, bool immediate = false)
		{
			// TODO: find state script, and call it's method

		}

		public void ExitVehicle(bool immediate = false)
		{
			if (!IsInVehicle || !IsInVehicleSeat)
				return;

			// this should be done only if player was a driver ?
			CurrentVehicle.StopControlling();

			if (IsLocalPlayer)
			{
				/*
                SendToServer(_lastPassengerState = new PlayerPassengerState {
                    Vechicle = null
                }, DeliveryMethod.ReliableOrdered, 1);
                */
			}
			else
			{
				//    _snapshots.Reset();
			}

			StartCoroutine (ExitVehicleAnimation (immediate));

		}

		private IEnumerator ExitVehicleAnimation(bool immediate)
		{
			IsInVehicleSeat = false;

			var seat = CurrentVehicleSeat;

			var animIndex = seat.IsLeftHand ? AnimIndex.GetOutLeft : AnimIndex.GetOutRight;

			PlayerModel.VehicleParentOffset = Vector3.Scale(PlayerModel.GetAnim(AnimGroup.Car, animIndex).RootStart, new Vector3(-1, -1, -1));

			if (!immediate)
			{
				var animState = PlayerModel.PlayAnim(AnimGroup.Car, animIndex, PlayMode.StopAll);
				animState.wrapMode = WrapMode.Once;

				// wait until anim finishes or stops
				while (animState.enabled)
					yield return new WaitForEndOfFrame();
			}

			// player now completely exited the vehicle

			PlayerModel.IsInVehicle = false;

			CurrentVehicle = null;
			CurrentVehicleSeat = null;
			seat.OccupyingPed = null;

			transform.localPosition = PlayerModel.VehicleParentOffset;
			transform.localRotation = Quaternion.identity;

			transform.SetParent(null);

			characterController.enabled = true;

			PlayerModel.VehicleParentOffset = Vector3.zero;

			// change camera parent
			if (IsLocalPlayer) {
				if (Camera != null) {
					Camera.transform.SetParent (null, true);
				}
			}

		}

		public static List<Vehicle.SeatAlignment> GetFreeSeats( Vehicle vehicle )
		{
			return vehicle.Seats.Where (s => !s.IsTaken).Select (s => s.Alignment).ToList ();

//			var freeSeats = new List<Vehicle.SeatAlignment> (vehicle.Seats.Select (s => s.Alignment));
//
//			var players = FindObjectsOfType<Player> ();
//
//			foreach (var p in players) {
//				if (p.IsInVehicle && p.CurrentVehicle == vehicle) {
//					freeSeats.Remove (p.CurrentVehicleSeatAlignment);
//				}
//			}
//
//			return freeSeats;
		}

		private void UpdateWheelTurning()
		{
			PlayerModel.VehicleParentOffset = Vector3.zero;

			var driveState = CurrentVehicle.Steering > 0 ? AnimIndex.DriveRight : AnimIndex.DriveLeft;

			var state = PlayerModel.PlayAnim(AnimGroup.Car, driveState, PlayMode.StopAll);

			state.speed = 0.0f;
			state.wrapMode = WrapMode.ClampForever;
			state.time = Mathf.Lerp(0.0f, state.length, Mathf.Abs(CurrentVehicle.Steering));
		}


		public Vehicle FindVehicleInRange ()
		{

			// find any vehicles that have a seat inside the checking radius and sort by closest seat
			return FindObjectsOfType<Vehicle>()
				.Where(x => Vector3.Distance(transform.position, x.FindClosestSeatTransform(transform.position).position) < EnterVehicleRadius)
				.OrderBy(x => Vector3.Distance(transform.position, x.FindClosestSeatTransform(transform.position).position))
				.FirstOrDefault();
			
		}

		public Vehicle TryEnterVehicleInRange ()
		{
			var vehicle = this.FindVehicleInRange ();
			if (null == vehicle)
				return null;

			var seat = vehicle.FindClosestSeat(this.transform.position);

			this.EnterVehicle(vehicle, seat);

			return vehicle;
		}

	}

}