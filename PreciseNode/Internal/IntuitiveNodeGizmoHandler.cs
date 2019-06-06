using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/******************************************************************************
 * Copyright (c) 2014-2016, Maik Schreiber
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/

namespace RegexKSP {
	// original code by TechnocratiK - http://bugs.kerbalspaceprogram.com/issues/3953
	internal class IntuitiveNodeGizmoHandler {
		private IntuitiveNodeGizmosManager intuitiveManeuvers;
		private ManeuverNode maneuverNode;
		private PreciseNodeOptions options;
		private Vector3d oldDV;
		private bool progradeChangeOccurred, normalChangeOccurred, radialChangeOccurred;

		public IntuitiveNodeGizmoHandler(IntuitiveNodeGizmosManager intuitiveManeuvers, ManeuverNode maneuverNode, PreciseNodeOptions options) {
			this.intuitiveManeuvers = intuitiveManeuvers;
			this.maneuverNode = maneuverNode;
			this.options = options;

			this.progradeChangeOccurred = false;
			this.radialChangeOccurred = false;
			this.normalChangeOccurred = false;

			// store the current value of the maneuver node
			this.oldDV = new Vector3d(this.maneuverNode.DeltaV.x,
				this.maneuverNode.DeltaV.y,
				this.maneuverNode.DeltaV.z);

			AttachHandleHandlers();
			this.maneuverNode.attachedGizmo.OnDelete += this.DeleteHandler;
		}

		public ManeuverNode ManeuverNode {
			get {
				return this.maneuverNode;
			}
		}

		public void OnProgradeChange(float value) {
			this.progradeChangeOccurred = true;
		}

		public void OnNormalChange(float value) {
			this.normalChangeOccurred = true;
		}

		public void OnRadialChange(float value) {
			this.radialChangeOccurred = true;
		}

		public void OnUpdate() {
			// this call is idempotent
			AttachHandleHandlers();

			if (!options.intuitiveManeuverGizmos) {
				return;
			}

			// if no user-invoked change occurred, return
			if (!(this.progradeChangeOccurred || this.radialChangeOccurred || this.normalChangeOccurred))
				return;

			Vector3d dV = this.maneuverNode.DeltaV;
			double UT = this.maneuverNode.UT;

			// get the orbital speed at the maneuver node's position
			double orbitalSpeed = this.maneuverNode.patch.getOrbitalVelocityAtUT(UT).magnitude;

			// compute the change in delta-V from the previous maneuver values
			Vector3d dDV = dV - this.oldDV;

			// get the true anomaly and eccentric anomaly of the current orbit at the maneuver
			double trueAnomaly = this.maneuverNode.patch.TrueAnomalyAtUT(UT),
				eccentricAnomaly = this.maneuverNode.patch.GetEccentricAnomaly(trueAnomaly);

			// get the semi-major and -minor axes, and eccentricity
			double semiMajor = this.maneuverNode.patch.semiMajorAxis,
				semiMinor = this.maneuverNode.patch.semiMinorAxis,
				eccentricity = this.maneuverNode.patch.eccentricity;

			// get the vessel-to-primary vector in the orbital plane
			Vector3d orbitalPlaneDownward = new Vector3d(semiMajor * (eccentricity - Math.Cos(eccentricAnomaly)),
				-semiMinor * Math.Sin(eccentricAnomaly));
			double coordinateRotation = -Math.Atan2(semiMinor * Math.Cos(eccentricAnomaly),
				-semiMajor * Math.Sin(eccentricAnomaly));

			// the maneuverDownward vector (in maneuver coordinates) is the axis that, along with
			// the prograde vector, forms the plane on which the radial direction must always exist
			Vector3d maneuverDownward = new Vector3d(-(Math.Sin(coordinateRotation) * orbitalPlaneDownward.x +
				Math.Cos(coordinateRotation) * orbitalPlaneDownward.y), 0.0,
				Math.Cos(coordinateRotation) * orbitalPlaneDownward.x -
				Math.Sin(coordinateRotation) * orbitalPlaneDownward.y);

			// with these vectors we compute the unit normal of the (old) target vector in maneuver
			// coordinates by computing the cross product
			Vector3d targetUnitNormal = (new Vector3d(-this.oldDV.y * maneuverDownward.z,
				this.oldDV.x * maneuverDownward.z - (this.oldDV.z + orbitalSpeed) * maneuverDownward.x,
				this.oldDV.y * maneuverDownward.x)).normalized;

			// similarly, the (old) target unit prograde
			Vector3d targetPrograde = new Vector3d(oldDV.x, oldDV.y, oldDV.z + orbitalSpeed);
			Vector3d targetUnitPrograde = targetPrograde.normalized;

			// and the (old) target unit radial out
			Vector3d targetUnitRadial = Vector3d.Cross(targetUnitNormal, targetUnitPrograde);

			// with all this information, compute the correction to the change in delta-V and apply
			// it to the maneuver node
			Vector3d correctedDDV = Vector3d.zero;

			if (this.progradeChangeOccurred) {
				// a prograde change occurred

				// this is the easiest one to do - simply compute the same change in the correct
				// direction
				correctedDDV = targetUnitPrograde * dDV.z;
			} else if (this.normalChangeOccurred) {
				// a normal change occurred

				// compute the vector in the target orbital plane parallel and orthogonal to the
				// vessel-planet (downward) vector
				Vector3d targetProgradeDownward = maneuverDownward * (Vector3d.Dot(targetPrograde, maneuverDownward) / maneuverDownward.sqrMagnitude);
				Vector3d targetProgradeLevel = targetPrograde - targetProgradeDownward;

				// an adjustment to the normal velocity equal to four times the (previous update's)
				// target prograde velocity (minus the downward component) is equivalent to two
				// complete direction reversals
				double dDVyRemainder = dDV.y % (4.0 * targetProgradeLevel.magnitude);

				if (dDVyRemainder > 2.0 * targetProgradeLevel.magnitude)
					dDVyRemainder -= 4.0 * targetProgradeLevel.magnitude;
				else if (dDVyRemainder < -2.0 * targetProgradeLevel.magnitude)
					dDVyRemainder += 4.0 * targetProgradeLevel.magnitude;

				// compute the angle by which the surface-parallel component of the prograde vector
				// rotates around the downward (i.e. the vessel-to-primary) axis
				double correctionAngle = 2.0 * Math.Asin(dDVyRemainder / (2.0 * targetProgradeLevel.magnitude));

				// compute the correction vector
				correctedDDV = (targetProgradeLevel.normalized * Math.Cos(correctionAngle) +
					targetUnitNormal * Math.Sin(correctionAngle)) * targetProgradeLevel.magnitude + targetProgradeDownward - oldDV;

				// additionally, subtract (back) out the orbital speed from the prograde component
				correctedDDV.z -= orbitalSpeed;
			} else if (this.radialChangeOccurred) {
				// a radial change occurred

				// an adjustment to the radial velocity equal to four times the (previous update's)
				// target prograde velocity is equivalent to two complete direction reversals
				double dDVxRemainder = dDV.x % (4.0 * targetPrograde.magnitude);

				if (dDVxRemainder > 2.0 * targetPrograde.magnitude)
					dDVxRemainder -= 4.0 * targetPrograde.magnitude;
				else if (dDVxRemainder < -2.0 * targetPrograde.magnitude)
					dDVxRemainder += 4.0 * targetPrograde.magnitude;

				// compute the angle by which the prograde vector rotates around the normal axis
				double correctionAngle = 2.0 * Math.Asin(dDVxRemainder / (2.0 * targetPrograde.magnitude));

				// compute the correction vector
				correctedDDV = (targetUnitPrograde * Math.Cos(correctionAngle) +
					targetUnitRadial * Math.Sin(correctionAngle)) * targetPrograde.magnitude - oldDV;

				// additionally, subtract (back) out the orbital speed from the prograde component
				correctedDDV.z -= orbitalSpeed;
			}

			// update the maneuver node
			this.maneuverNode.DeltaV = oldDV + correctedDDV;
			this.maneuverNode.attachedGizmo.DeltaV = oldDV + correctedDDV;
			this.maneuverNode.OnGizmoUpdated(oldDV + correctedDDV, UT);

			// store the latest maneuver node values
			this.oldDV += correctedDDV;

			// reset the flags
			this.progradeChangeOccurred = false;
			this.normalChangeOccurred = false;
			this.radialChangeOccurred = false;
		}

		public void DeleteHandler() {
			DetachHandleHandlers();
			this.maneuverNode.attachedGizmo.OnDelete -= this.DeleteHandler;

			// remove this handler from the addin's list
			this.intuitiveManeuvers.RemoveIntuitiveManeuverHandler(this);
		}

		private void AttachHandleHandlers() {

            if (this.maneuverNode.attachedGizmo == null)
                return;

            if (this.maneuverNode.attachedGizmo.handlePrograde != null &&
				!this.maneuverNode.attachedGizmo.handlePrograde.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnProgradeChange)))
				this.maneuverNode.attachedGizmo.handlePrograde.OnHandleUpdate += this.OnProgradeChange;

			if (this.maneuverNode.attachedGizmo.handleRetrograde != null &&
				!this.maneuverNode.attachedGizmo.handleRetrograde.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnProgradeChange)))
				this.maneuverNode.attachedGizmo.handleRetrograde.OnHandleUpdate += this.OnProgradeChange;

			if (this.maneuverNode.attachedGizmo.handleNormal != null &&
				!this.maneuverNode.attachedGizmo.handleNormal.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnNormalChange)))
				this.maneuverNode.attachedGizmo.handleNormal.OnHandleUpdate += this.OnNormalChange;

			if (this.maneuverNode.attachedGizmo.handleAntiNormal != null &&
				!this.maneuverNode.attachedGizmo.handleAntiNormal.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnNormalChange)))
				this.maneuverNode.attachedGizmo.handleAntiNormal.OnHandleUpdate += this.OnNormalChange;

			if (this.maneuverNode.attachedGizmo.handleRadialIn != null &&
				!this.maneuverNode.attachedGizmo.handleRadialIn.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnRadialChange)))
				this.maneuverNode.attachedGizmo.handleRadialIn.OnHandleUpdate += this.OnRadialChange;

			if (this.maneuverNode.attachedGizmo.handleRadialOut != null &&
				!this.maneuverNode.attachedGizmo.handleRadialOut.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnRadialChange)))
				this.maneuverNode.attachedGizmo.handleRadialOut.OnHandleUpdate += this.OnRadialChange;
		}

		private void DetachHandleHandlers() {
            if (this.maneuverNode.attachedGizmo == null)
                return;
            
			if (this.maneuverNode.attachedGizmo.handlePrograde != null &&
				this.maneuverNode.attachedGizmo.handlePrograde.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnProgradeChange)))
				this.maneuverNode.attachedGizmo.handlePrograde.OnHandleUpdate -= this.OnProgradeChange;

            if (this.maneuverNode.attachedGizmo.handleRetrograde != null &&
				this.maneuverNode.attachedGizmo.handleRetrograde.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnProgradeChange)))
				this.maneuverNode.attachedGizmo.handleRetrograde.OnHandleUpdate -= this.OnProgradeChange;

            if (this.maneuverNode.attachedGizmo.handleNormal != null &&
				this.maneuverNode.attachedGizmo.handleNormal.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnNormalChange)))
				this.maneuverNode.attachedGizmo.handleNormal.OnHandleUpdate -= this.OnNormalChange;

            if (this.maneuverNode.attachedGizmo.handleAntiNormal != null &&
				this.maneuverNode.attachedGizmo.handleAntiNormal.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnNormalChange)))
				this.maneuverNode.attachedGizmo.handleAntiNormal.OnHandleUpdate -= this.OnNormalChange;

            if (this.maneuverNode.attachedGizmo.handleRadialIn != null &&
				this.maneuverNode.attachedGizmo.handleRadialIn.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnRadialChange)))
				this.maneuverNode.attachedGizmo.handleRadialIn.OnHandleUpdate -= this.OnRadialChange;

            if (this.maneuverNode.attachedGizmo.handleRadialOut != null &&
				this.maneuverNode.attachedGizmo.handleRadialOut.OnHandleUpdate.GetInvocationList().Contains(
				new ManeuverGizmoHandle.HandleUpdate(this.OnRadialChange)))
				this.maneuverNode.attachedGizmo.handleRadialOut.OnHandleUpdate -= this.OnRadialChange;
		}
	}
}
