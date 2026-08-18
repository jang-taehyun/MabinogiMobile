using CoreModule;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MabinogiMobileServer
{
    class Player : IDisposable
    {
        public static int SerializeSize
        {
            get
            {
                return sizeof(int) + sizeof(float) * 3 + sizeof(float) * 3;
            }
        }

        public required int PlayerID { get; init; }

        public float[] Position { get; set; } = new float[3];
        public float[] Forward { get; set; } = new float[3];

        public required Socket ClientSocket { get; init; }

        public void SerializePlayerInfo(Span<byte> buffer)
        {
            int position = 0;

            // serialize player ID
            BitConverter.TryWriteBytes(buffer.Slice(position, sizeof(int)), PlayerID);
            position += sizeof(int);

            // serialize position
            foreach (float value in Position)
            {
                BitConverter.TryWriteBytes(buffer.Slice(position, sizeof(float)), value);
                position += sizeof(float);
            }

            // serialize forward vector
            foreach (float value in Forward)
            {
                BitConverter.TryWriteBytes(buffer.Slice(position, sizeof(float)), value);
                position += sizeof(float);
            }
        }

        // move & rotate //
        private static float S_FPT = 0.01667f;
        private static int INT_FPS = 16;
        private CancellationTokenSource MoveCancelToken = new CancellationTokenSource();
        public void MovePlayer(float[] position, float[] forward)
        {
            // check current position
            if (Position.Length != position.Length) return;
            for (int i = 0; i < Position.Length; ++i)
                if (MathF.Abs(Position[i] - position[i]) > 0.001f) return;

            // modify character's forward vector
            Forward = forward;

            // move character & run move player at interval
            MoveCancelToken.Cancel();
            _ = MovePlayerAtInterval(this, MoveCancelToken.Token);
        }
        private static async Task MovePlayerAtInterval(Player movePlayer, CancellationToken token)
        {
            while (token.IsCancellationRequested is false)
            {
                // compute target position
                float[] targetPosition = new float[3];
                for (int i = 0; i < 3; ++i)
                    targetPosition[i] = movePlayer.Position[i] + movePlayer.Forward[i] * S_FPT;

                // modify character position
                movePlayer.Position = targetPosition;

                // broadcast target position, forward
                NetworkManager.Instance.Broadcast(
                    PacketID.PlayerMoving,
                    new PlayerMovingPacket(movePlayer.PlayerID, movePlayer.Position, movePlayer.Forward),
                    movePlayer.PlayerID
                );

                await Task.Delay(INT_FPS, token);
            }
        }
        public void EndMovePlayer(float[] position, float[] forward)
        {
            bool IsPacketValid = true;

            // check position, forward
            for (int i = 0; i < Position.Length; ++i)
                if (MathF.Abs(Position[i] - position[i]) > 0.001f)
                {
                    IsPacketValid = false;
                    break;
                }
            for (int i = 0; i < Forward.Length; ++i)
                if (MathF.Abs(Forward[i] - forward[i]) > 0.001f)
                {
                    IsPacketValid = false;
                    break;
                }

            // send packet to player
            if (IsPacketValid is true)
            {
                _ = NetworkManager.Instance.SendPacket
                    (
                        PacketID.Transform,
                        new TransformPacket(PlayerID, Position, Forward),
                        ClientSocket
                    );
            }

            // cancel move player at inteval
            MoveCancelToken.Cancel();

            // broadcast player end move packet
            NetworkManager.Instance.Broadcast(
                    PacketID.PlayerMoveEnd,
                    new PlayerMoveEndPacket(PlayerID, Position, Forward),
                    PlayerID
            );
        }

        public void Dispose()
        {
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();

            MoveCancelToken.Cancel();
            MoveCancelToken.Dispose();
        }
    }
}
