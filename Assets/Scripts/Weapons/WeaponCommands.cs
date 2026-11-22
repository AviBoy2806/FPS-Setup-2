// Each command represents one request. The player is the receiver.
public interface IWeaponCommand
{
    void Execute(Player player);
}

public sealed class StartFireCommand : IWeaponCommand
{
    public void Execute(Player player) => player.RequestStartFire();
}

public sealed class StopFireCommand : IWeaponCommand
{
    public void Execute(Player player) => player.RequestStopFire();
}

public sealed class ReloadCommand : IWeaponCommand
{
    public void Execute(Player player) => player.RequestReload();
}

public sealed class ToggleDamageCommand : IWeaponCommand
{
    public void Execute(Player player) => player.RequestToggleDamage();
}

public sealed class ToggleFastReloadCommand : IWeaponCommand
{
    public void Execute(Player player) => player.RequestToggleFastReload();
}

// The invoker runs commands immediately. It does not need a queue.
public sealed class WeaponCommandInvoker
{
    private readonly Player player;

    public WeaponCommandInvoker(Player player)
    {
        this.player = player;
    }

    public void Execute(IWeaponCommand command)
    {
        if (player != null && command != null)
            command.Execute(player);
    }
}
