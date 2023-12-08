using UnityEngine;

public class PositionSignal {
	private struct Frame {
		public Vector3 Position;
		public double Time;
		public Frame(Vector3 position, double time) {
			Position = position;
			Time = time;
		}
	}
	private Frame _previousFrame;
	private Frame _currentFrame;

	public PositionSignal(Vector3 startPosition, double time) {
		_currentFrame = _previousFrame = new Frame {
			Position = startPosition,
			Time = time,
		};
	}

	public void SetPosition(Vector3 position, double time) {
		_previousFrame = _currentFrame;
		_currentFrame = new Frame {
			Position = position,
			Time = time,
		};
	}

	public Vector3 GetCurrent() => _currentFrame.Position;
	public Vector3 GetPrevious() => _previousFrame.Position;

	public Vector3 SamplePosition(double time) {
		var diff = _currentFrame.Time - _previousFrame.Time;
		if (diff == 0) {
			return _previousFrame.Position;
		}
		double t = ((double)(time) - (double)_previousFrame.Time) / (double)diff;
		return Vector3.Lerp(_previousFrame.Position,_currentFrame.Position, (float)t);
	}
}