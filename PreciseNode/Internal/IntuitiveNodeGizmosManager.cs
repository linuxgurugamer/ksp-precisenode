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
	internal class IntuitiveNodeGizmosManager {
		private PreciseNodeOptions options;

		// as far as I know, it's not possible for more than one maneuver gizmo to be visible at
		// any one time in the planetarium, but just in case, we maintain a list of gizmo handlers
		private List<IntuitiveNodeGizmoHandler> maneuverGizmoHandlers;

		public IntuitiveNodeGizmosManager(PreciseNodeOptions options) {
			this.options = options;
			this.maneuverGizmoHandlers = new List<IntuitiveNodeGizmoHandler>();
		}

		internal void OnUpdate() {
			this.UpdateIntuitiveManeuverHandlersList();

			// iterate over the current handlers
			for (int i = 0; i < maneuverGizmoHandlers.Count; i++) {
				maneuverGizmoHandlers[i].OnUpdate();
			}
		}

		internal void OnDestroy() {
			while (this.maneuverGizmoHandlers.Count > 0)
				this.maneuverGizmoHandlers[0].DeleteHandler();
		}

		PatchedConicSolver curSolver = null;
		private void UpdateIntuitiveManeuverHandlersList() {
			PatchedConicSolver solver = NodeTools.getSolver();
			if (solver != curSolver)
			{
				this.maneuverGizmoHandlers.Clear();
				curSolver = solver;
				if (solver != null)
				{
					List<ManeuverNode> nodes = solver.maneuverNodes;
					for (int i = 0; i < nodes.Count; i++)
					{
						ManeuverNode node = nodes[i];
						if ((node.attachedGizmo != null) && !isHandled(node))
						{
							this.maneuverGizmoHandlers.Add(new IntuitiveNodeGizmoHandler(this, node, options));
						}
					}
				}
			}
		}

		private bool isHandled(ManeuverNode node) {
			for (int i = 0; i < maneuverGizmoHandlers.Count; i++) {
				if (node == maneuverGizmoHandlers[i].ManeuverNode) {
					return true;
				}
			}
			return false;
		}

		internal void RemoveIntuitiveManeuverHandler(IntuitiveNodeGizmoHandler intuitiveManeuverHandler) {
			this.maneuverGizmoHandlers.Remove(intuitiveManeuverHandler);
		}
	}
}
