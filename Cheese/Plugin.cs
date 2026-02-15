using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Cheese;

[ApiVersion(2, 1)]
// ReSharper disable once UnusedType.Global
public class Plugin(Main game) : TerrariaPlugin(game)
{
    public override string Name => "Cheese";
    public override string Author => "Cai";
    public override string Description => "拍照用的插件";
    public override Version Version => new(1, 1);

    public override void Initialize()
    {
        Commands.ChatCommands.Add(new Command(Permission.Admin, CheeseCommand, "cheese", "cz"));
        GetDataHandlers.NewProjectile.Register(OnNewProjectile);
        GetDataHandlers.ItemDrop.Register(OnItemDrop);
        ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpawn);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == CheeseCommand);
        GetDataHandlers.NewProjectile.UnRegister(OnNewProjectile);
        GetDataHandlers.ItemDrop.UnRegister(OnItemDrop);
        ServerApi.Hooks.NpcSpawn.Deregister(this, OnNpcSpawn);
    }

    private static void OnNpcSpawn(NpcSpawnEventArgs args)
    {
        if (!Setting.LimitNpcSpawn) return;

        var npc = Main.npc[args.NpcId];
        npc.type = 0;
        npc.active = false;
        TSPlayer.All.SendData(PacketTypes.NpcUpdate, null, args.NpcId);
        args.Handled = true;
    }

    private static void OnItemDrop(object? sender, GetDataHandlers.ItemDropEventArgs e)
    {
        if (!Setting.LimitItemDrop ||
            (e.Player.HasPermission(Permission.ByPass) || e.Player.HasPermission(Permission.Admin))) return;

        e.Player.SendData(PacketTypes.SyncItemDespawn, null, e.ID);
        e.Handled = true;
    }

    private static void OnNewProjectile(object? sender, GetDataHandlers.NewProjectileEventArgs e)
    {
        if (!Setting.LimitProjectile ||
            (e.Player.HasPermission(Permission.ByPass) || e.Player.HasPermission(Permission.Admin))) return;

        e.Player.RemoveProjectile(e.Identity, e.Owner);
        e.Handled = true;
    }

    private static void CheeseCommand(CommandArgs args)
    {
        if (args.Parameters.Count < 1)
        {
            SendHelp();
            return;
        }

        switch (args.Parameters[0])
        {
            case "help":
                SendHelp();
                break;
            case "proj":
                Setting.LimitProjectile = !Setting.LimitProjectile;
                args.Player.SendSuccessMessage($"[Cheese]弹幕限制已{(Setting.LimitProjectile ? "启用" : "关闭")}!");
                break;
            case "item":
                Setting.LimitItemDrop = !Setting.LimitItemDrop;
                args.Player.SendSuccessMessage($"[Cheese]掉落物限制已{(Setting.LimitItemDrop ? "启用" : "关闭")}!");
                break;
            case "npc":
                Setting.LimitNpcSpawn = !Setting.LimitNpcSpawn;
                args.Player.SendSuccessMessage($"[Cheese]生物生成限制已{(Setting.LimitNpcSpawn ? "启用" : "关闭")}!");
                break;
            case "clear":
                var clearedProjectileCount = 0;
                for (var i = 0; i < Main.maxProjectiles; ++i)
                {
                    if (!Main.projectile[i].active) continue;

                    Main.projectile[i].active = false;
                    Main.projectile[i].type = 0;
                    TSPlayer.All.SendData(PacketTypes.ProjectileNew, null, i);
                    clearedProjectileCount++;
                }

                var clearedItemCount = 0;
                for (var i = 0; i < Main.maxItems; ++i)
                {
                    if (!Main.item[i].active) continue;

                    Main.item[i].TurnToAir();
                    TSPlayer.All.SendData(PacketTypes.SyncItemDespawn, null, i);
                    clearedItemCount++;
                }

                args.Player.SendSuccessMessage($"[Cheese]清理了{clearedProjectileCount}个弹幕和{clearedItemCount}个掉落物!");
                break;
            case "st":
                args.Player.SendWarningMessage("[Cheese]\n" +
                                               $"限制弹幕(proj): {(Setting.LimitProjectile ? "启用" : "关闭")}\n" +
                                               $"限制掉落物(item): {(Setting.LimitItemDrop ? "启用" : "关闭")}\n" +
                                               $"限制生物生成(npc): {(Setting.LimitNpcSpawn ? "启用" : "关闭")}");
                break;
        }

        return;

        void SendHelp()
        {
            args.Player.SendWarningMessage("[Cheese]\n" +
                                           "proj  --- 限制弹幕\n" +
                                           "item  --- 限制掉落物\n" +
                                           "npc   --- 限制生物生成\n" +
                                           "clear --- 清理掉落物和弹幕\n" +
                                           "st    --- 查看状态");
        }
    }
}