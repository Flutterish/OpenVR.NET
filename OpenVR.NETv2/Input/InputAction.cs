namespace OpenVR.NET.Input;

/// <inheritdoc/>
public abstract class InputAction<T> : Action where T : struct {
	private T value;
	public T Value {
		get => value;
		protected set {
			if ( value.Equals( this.value ) )
				return;

			var prev = this.value;
			this.value = value;

			ValueChanged?.Invoke( prev, value );
			ValueUpdated?.Invoke( value );
		}
	}

	public delegate void ValueChangedHandler ( T oldValue, T newValue );
	public event ValueChangedHandler? ValueChanged;
	public event Action<T>? ValueUpdated;
}
