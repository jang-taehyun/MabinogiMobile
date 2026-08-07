
namespace CoreModule
{
    public interface IPacketHandler
    {
        void ProcessPacket(IPacket Packet);

        public static T? CheckPacket<T>(IPacket packet) where T : class, IPacket
        {
            T? ret = null;
            try
            {
                if (packet == null)
                    throw new MobinogiException("packet is null");
                if ((packet is T) == false)
                    throw new MobinogiException("packet is difference type");

                ret = (T)packet;
            }
            catch (MobinogiException e)
            {
                e.OutputExceptionLog();
            }

            return ret;
        }
    }

    public class PacketHandlerInvoker
    {
        public static void ProcessPacket<T>(IPacket Packet) where T : class, IPacketHandler, new() => new T().ProcessPacket(Packet);
    }
}
