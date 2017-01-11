using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class State {

	public Vector3 position;
	public List<Vector3> path;
	public List<Vector3> space;

	bool isContiguous = true;
	bool noTwoDeadEnds = true;
	public bool yellowCard = false;

	public State(Vector3 pos, List<Vector3> pth) {
		position = pos;
		path = pth;

		space = new List<Vector3>();
		for (int i = 0; i < 3; i++) {
			for (int j = 0; j < 3; j++) {
				for (int k = 0; k < 3; k++) {
					if (!path.Contains(new Vector3(i,j,k))) {
						space.Add(new Vector3(i,j,k));
					}
				}
			}
		}
		space.Add(position);

		// CONTIGUITY
		isContiguous = ContiguityCheck();

		// NO TWO DEAD ENDS
		noTwoDeadEnds = (Tools.DeadEndCount(space, position) < 2);

		// MAX DEAD ENDS AFTER REMOVAL

	}

	bool ContiguityCheck() {
		List<Vector3> reached = new List<Vector3>();
		List<Vector3> targets = new List<Vector3>();
		bool output = true;

		if (space.Count > 0) {
			targets.Add(space[0]);
		}
		while (targets.Count != 0) {
			Vector3 current = targets[0];
			targets.RemoveAt(0);
			reached.Add(current);
			foreach (Vector3 p in current.GetAdjacent()) {
				if (space.Contains(p) && !reached.Contains(p) && !targets.Contains(p)) {
					targets.Add(p);
				}
			}
		}
		foreach (Vector3 p in space) {
			if (!reached.Contains(p)) {
				output = false;
				break;
			}
		}
		return output;
	}

	int DeadEndCount() {
		int neighborsi = 0;
		int output = 0;
		foreach (Vector3 posi in space) {
			if (posi != position) {
				foreach (Vector3 posj in space) {
					if (Vector3.Magnitude(posi-posj) == 1) {
						neighborsi++;
					}
				}
				if (neighborsi < 2) {
					output++;
				}
				neighborsi = 0;
			}
		}
		return output;
	}

	/// <summary>
	/// Gets all successors of state.
	/// </summary>
	/// <returns>The successors.</returns>
	/// <param name="dimension">Dimension of puzzle.</param>
	public List<State> GetSuccessors(int dimension = 3) {
		List<State> output = new List<State>();
		if (Vector3.Magnitude(position) == 0) {
			List<Vector3> pth = new List<Vector3>(path);
			Vector3 pos = position+Vector3.right;
			pth.Add(pos);
			output.Add(new State(pos, pth));
			return output;
		}
		if (position.x > 0 && !path.Contains(position+Vector3.left)) {
			List<Vector3> pth = new List<Vector3>(path);
			Vector3 pos = position+Vector3.left;
			pth.Add(pos);
			output.Add(new State(pos, pth));
		}
		if (position.x < dimension-1 && !path.Contains(position+Vector3.right)) {
			List<Vector3> pth = new List<Vector3>(path);
			Vector3 pos = position+Vector3.right;
			pth.Add(pos);
			output.Add(new State(pos, pth));
		}
		if (position.y > 0 && !path.Contains(position+Vector3.down)) {
			List<Vector3> pth = new List<Vector3>(path);
			Vector3 pos = position+Vector3.down;
			pth.Add(pos);
			output.Add(new State(pos, pth));
		}
		if (position.y < dimension-1 && !path.Contains(position+Vector3.up)) {
			List<Vector3> pth = new List<Vector3>(path);
			Vector3 pos = position+Vector3.up;
			pth.Add(pos);
			output.Add(new State(pos, pth));
		}
		if (position.z > 0 && !path.Contains(position+Vector3.back)) {
			List<Vector3> pth = new List<Vector3>(path);
			Vector3 pos = position+Vector3.back;
			pth.Add(pos);
			output.Add(new State(pos, pth));
		}
		if (position.z < dimension-1 && !path.Contains(position+Vector3.forward)) {
			List<Vector3> pth = new List<Vector3>(path);
			Vector3 pos = position+Vector3.forward;
			pth.Add(pos);
			output.Add(new State(pos, pth));
		}
		output.Shuffle();
		return output;
	}

	/// <summary>
	/// Gets filtered list of successors.
	/// </summary>
	/// <returns>The successors.</returns>
	/// <param name="dimension">Dimension of puzzle.</param>
	public List<State> SmartSuccessors(int dimension = 3) {
		List<State> output = new List<State>();
		List<State> dumbs = this.GetSuccessors(dimension);


		foreach (State dumb in dumbs) {
			if (dumb.isValid()) {
				output.Add(dumb);
			}
		}
		return output;
	}

	/// <summary>
	/// Checks if state is a goal state.
	/// </summary>
	/// <returns><c>true</c>, if state is goal, <c>false</c> otherwise.</returns>
	/// <param name="dimension">Dimension.</param>
	public bool isGoalState(int dimension = 3) {
		if (path.Count == dimension*dimension*dimension) {
			return true;
		} else {
			return false;
		}
	}

	/// <summary>
	/// Checks if state is valid.
	/// </summary>
	/// <returns><c>true</c>, if valid was ised, <c>false</c> otherwise.</returns>
	public bool isValid() {
		return (isContiguous && noTwoDeadEnds);
	}
}
