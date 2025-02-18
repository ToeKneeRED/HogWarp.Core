// This file is automatically generated

#pragma warning disable CS8618
#pragma warning disable CS0219

using HogWarpSdk;
using HogWarpSdk.Game;
using HogWarpSdk.Systems;
using HogWarpSdk.Internal;

namespace HogWarp.Replicated
{
    [Replicated(Class = "/HogWarpChat/Actors/BP_HogWarpChat.BP_HogWarpChat", Hash = 15185056061269658332)]
    public partial class BP_HogWarpChat : Actor
    {
        public partial void SendMsg(Player player, string Message);

        [ServerRpc(Function = "SendMsg", Hash = 3936215300427668376)]
        public void SendMsg_Impl(Player player, IBuffer data)
        {
            ushort length = 0;
            var Message = data.ReadString();
            
            SendMsg(player, Message);
        }
        
        [ClientRpc(Function = "RecieveMsg", Hash = 12684788271436983199)]
        public void RecieveMsg(Player player, string Message)
        {
            ushort length = 0;
            var data = IBuffer.Create();
            try
            {
                data.WriteString(Message);
                
                IRpc.Get().Call(player.InternalPlayer, Id, 15185056061269658332, 12684788271436983199, data);
            }
            finally
            {
                IBuffer.Release(data);
            }
        }
    }
}
