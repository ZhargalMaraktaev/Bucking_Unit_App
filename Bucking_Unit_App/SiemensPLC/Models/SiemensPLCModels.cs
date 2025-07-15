using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using LiveChartsCore.SkiaSharpView;
using PdfSharpCore.Pdf.IO;
using Sharp7;

namespace Bucking_Unit_App.SiemensPLC.Models
{
    public partial class SiemensPLCModels
    {
        public partial class DBAddressModel
        {
            public class DB100DBD4 : DbbDbwDbdAddress
            {
                public DB100DBD4(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "DB100.DBD4";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 100;
                protected override int Offset { get; } = 4;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class DB100DBD24 : DbbDbwDbdAddress
            {
                public DB100DBD24(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "DB100.DBD24";
                protected override Types Type { get; } = Types.Dint;
                protected override int Number { get; } = 100;
                protected override int Offset { get; } = 24;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class DB100DBW52 : DbbDbwDbdAddress
            {
                public DB100DBW52(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "DB100.DBW52";
                protected override Types Type { get; } = Types.Int;
                protected override int Number { get; } = 100;
                protected override int Offset { get; } = 52;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 2;
            }

            public class DB100DBD82 : DbbDbwDbdAddress
            {
                public DB100DBD82(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "DB100.DBD82";
                protected override Types Type { get; } = Types.Lreal;
                protected override int Number { get; } = 100;
                protected override int Offset { get; } = 82;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 8;
            }

            public class DB100DBD164 : DbbDbwDbdAddress
            {
                public DB100DBD164(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "DB100.DBD164";
                protected override Types Type { get; } = Types.Udint;
                protected override int Number { get; } = 100;
                protected override int Offset { get; } = 164;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class DB100DBX66_0 : DbxStringAddress
            {
                public DB100DBX66_0(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "DB100.DBX66.0";
                protected override Types Type { get; } = Types.StringASCII;
                protected override int Number { get; } = 100;
                protected override int Offset { get; } = 66;
                protected override int Pos { get; } = 0;
                protected override int MaxLength { get; } = 10;
            }

            public class DB100DBWString130 : DbwStringAddress
            {
                public DB100DBWString130(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "DB100.DBWString130";
                protected override Types Type { get; } = Types.WString;
                protected override int Number { get; } = 100;
                protected override int Offset { get; } = 130;
                protected override int MaxLength { get; } = 10;
            }

            public class ActualTorqueHMI : DbbDbwDbdAddress
            {
                public ActualTorqueHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD0";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 0;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class InternalTorqueHMI : DbbDbwDbdAddress
            {
                public InternalTorqueHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD4";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 4;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class FrontClampFDHMI : DbbDbwDbdAddress
            {
                public FrontClampFDHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD8";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 8;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialClampFDHMI : DbbDbwDbdAddress
            {
                public AxialClampFDHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD12";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 12;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class DownRotationFDHMI : DbbDbwDbdAddress
            {
                public DownRotationFDHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD16";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 16;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class UnalignedClampFDHMI : DbbDbwDbdAddress
            {
                public UnalignedClampFDHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD20";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 20;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class PressureFDHMI : DbbDbwDbdAddress
            {
                public PressureFDHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD24";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 24;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class OilTemperatureHMI : DbbDbwDbdAddress
            {
                public OilTemperatureHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD28";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 28;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class OilPressureNo1HMI : DbbDbwDbdAddress
            {
                public OilPressureNo1HMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD32";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 32;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class DownRotationPressureHMI : DbbDbwDbdAddress
            {
                public DownRotationPressureHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD36";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 36;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialDisplacementHMI : DbbDbwDbdAddress
            {
                public AxialDisplacementHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD40";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 40;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class LateralDisplacementHMI : DbbDbwDbdAddress
            {
                public LateralDisplacementHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD44";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 44;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class VerticalDisplacementHMI : DbbDbwDbdAddress
            {
                public VerticalDisplacementHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD48";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 48;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialAccelerationHMI : DbbDbwDbdAddress
            {
                public AxialAccelerationHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD52";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 52;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class HousingHMI : DbbDbwDbdAddress
            {
                public HousingHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD56";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 56;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class QuantityHMI : DbbDbwDbdAddress
            {
                public QuantityHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD60";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 60;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class IdleTorqueHMI : DbbDbwDbdAddress
            {
                public IdleTorqueHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD64";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 64;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StartingTorqueHMI : DbbDbwDbdAddress
            {
                public StartingTorqueHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD68";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 68;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class TorqueUpperLimitHMI : DbbDbwDbdAddress
            {
                public TorqueUpperLimitHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD72";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 72;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class TorqueLowerLimitHMI : DbbDbwDbdAddress
            {
                public TorqueLowerLimitHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD76";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 76;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class FeedDelayTimeHMI : DbbDbwDbdAddress
            {
                public FeedDelayTimeHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD80";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 80;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class RPMUpperLimitHMI : DbbDbwDbdAddress
            {
                public RPMUpperLimitHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD84";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 84;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class RPMLowerLimitHMI : DbbDbwDbdAddress
            {
                public RPMLowerLimitHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD88";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 88;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class OptimalRPMHMI : DbbDbwDbdAddress
            {
                public OptimalRPMHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD92";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 92;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class BatchNumberHMI : DbbDbwDbdAddress
            {
                public BatchNumberHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD98";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 98;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class CuringSpeedHMI : DbbDbwDbdAddress
            {
                public CuringSpeedHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD102";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 102;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentSpeedHMI : DbbDbwDbdAddress
            {
                public AlignmentSpeedHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD106";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 106;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class CuringRotationCount : DbbDbwDbdAddress
            {
                public CuringRotationCount(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD110";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 110;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentRotationCount : DbbDbwDbdAddress
            {
                public AlignmentRotationCount(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD114";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 114;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentPressureSetpointHMI : DbbDbwDbdAddress
            {
                public AlignmentPressureSetpointHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD118";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 118;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StructureLengthHMI : DbbDbwDbdAddress
            {
                public StructureLengthHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD122";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 122;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentCoefficientHMI : DbbDbwDbdAddress
            {
                public AlignmentCoefficientHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD126";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 126;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class VerticalPositioningHMI : DbbDbwDbdAddress
            {
                public VerticalPositioningHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD130";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 130;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialMovement2 : DbbDbwDbdAddress
            {
                public AxialMovement2(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD134";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 134;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialMovement21 : DbbDbwDbdAddress
            {
                public AxialMovement21(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD138";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 138;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialMovementCoefficient : DbbDbwDbdAddress
            {
                public AxialMovementCoefficient(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD142";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 142;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class RetractDisplacementSetHMI : DbbDbwDbdAddress
            {
                public RetractDisplacementSetHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD146";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 146;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class LateralDeviationHMI : DbbDbwDbdAddress
            {
                public LateralDeviationHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD150";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 150;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class FrontSlidingHMI : DbbDbwDbdAddress
            {
                public FrontSlidingHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD154";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 154;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class RearSlidingHMI : DbbDbwDbdAddress
            {
                public RearSlidingHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD158";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 158;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialSpeed1HMI : DbbDbwDbdAddress
            {
                public AxialSpeed1HMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD162";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 162;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialSpeed3HMI : DbbDbwDbdAddress
            {
                public AxialSpeed3HMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD166";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 166;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialSpeed4HMI : DbbDbwDbdAddress
            {
                public AxialSpeed4HMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD170";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 170;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialSpeed5HMI : DbbDbwDbdAddress
            {
                public AxialSpeed5HMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD174";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 174;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialRetractSpeed2HMI : DbbDbwDbdAddress
            {
                public AxialRetractSpeed2HMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD178";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 178;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialRetractSpeed3HMI : DbbDbwDbdAddress
            {
                public AxialRetractSpeed3HMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD182";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 182;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class FrontClampSpeedHMI : DbbDbwDbdAddress
            {
                public FrontClampSpeedHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD186";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 186;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class UnalignedQuantityHMI : DbbDbwDbdAddress
            {
                public UnalignedQuantityHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD190";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 190;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentClampHMI : DbbDbwDbdAddress
            {
                public AlignmentClampHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD194";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 194;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentPressureCoefficientHMI : DbbDbwDbdAddress
            {
                public AlignmentPressureCoefficientHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD198";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 198;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class TorqueLeakage : DbbDbwDbdAddress
            {
                public TorqueLeakage(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD202";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 202;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentIndicator : DbbDbwDbdAddress
            {
                public AlignmentIndicator(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD206";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 206;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StatusValue : DbbDbwDbdAddress
            {
                public StatusValue(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD210";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 210;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StatusTime : DbbDbwDbdAddress
            {
                public StatusTime(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD214";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 214;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StatusUpperLimit : DbbDbwDbdAddress
            {
                public StatusUpperLimit(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD218";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 218;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StatusLowerLimit : DbbDbwDbdAddress
            {
                public StatusLowerLimit(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD222";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 222;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class ReturnDelayTimeHMI : DbbDbwDbdAddress
            {
                public ReturnDelayTimeHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD226";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 226;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentCenterHeight : DbbDbwDbdAddress
            {
                public AlignmentCenterHeight(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD230";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 230;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class CuringCenterHeight : DbbDbwDbdAddress
            {
                public CuringCenterHeight(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD234";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 234;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class ThroughHoleCenterHeight : DbbDbwDbdAddress
            {
                public ThroughHoleCenterHeight(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD238";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 238;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AlignmentCenterHeightSetpoint : DbbDbwDbdAddress
            {
                public AlignmentCenterHeightSetpoint(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD242";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 242;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class CuringCenterHeightSetpoint : DbbDbwDbdAddress
            {
                public CuringCenterHeightSetpoint(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD246";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 246;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class ThroughHoleCenterHeightSetpoint : DbbDbwDbdAddress
            {
                public ThroughHoleCenterHeightSetpoint(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD250";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 250;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class TubeInnerDiameter : DbbDbwDbdAddress
            {
                public TubeInnerDiameter(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD254";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 254;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class TubeLateralLength : DbbDbwDbdAddress
            {
                public TubeLateralLength(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD258";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 258;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class TubeThroughHoleLength : DbbDbwDbdAddress
            {
                public TubeThroughHoleLength(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD262";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 262;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class CuringData : DbbDbwDbdAddress
            {
                public CuringData(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD266";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 266;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class SurplusData : DbbDbwDbdAddress
            {
                public SurplusData(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD270";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 270;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class ClampingHoleOffset : DbbDbwDbdAddress
            {
                public ClampingHoleOffset(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD274";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 274;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class MinimumData : DbbDbwDbdAddress
            {
                public MinimumData(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD278";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 278;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class MaximumData : DbbDbwDbdAddress
            {
                public MaximumData(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD282";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 282;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class SetData : DbbDbwDbdAddress
            {
                public SetData(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD286";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 286;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class CuringModel : DbbDbwDbdAddress
            {
                public CuringModel(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD290";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 290;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class LateralMovementSpeed : DbbDbwDbdAddress
            {
                public LateralMovementSpeed(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD294";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 294;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class RotationSpeed : DbbDbwDbdAddress
            {
                public RotationSpeed(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD298";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 298;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialSpeed1 : DbbDbwDbdAddress
            {
                public AxialSpeed1(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD302";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 302;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class AxialSpeed2 : DbbDbwDbdAddress
            {
                public AxialSpeed2(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD306";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 306;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class UnalignedCounter : DbbDbwDbdAddress
            {
                public UnalignedCounter(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD310";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 310;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class OilNozzleLength : DbbDbwDbdAddress
            {
                public OilNozzleLength(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD314";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 314;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class CalibrationSetting : DbbDbwDbdAddress
            {
                public CalibrationSetting(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD318";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 318;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class DataSetting : DbbDbwDbdAddress
            {
                public DataSetting(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD322";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 322;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class MinimumData2 : DbbDbwDbdAddress
            {
                public MinimumData2(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD326";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 326;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class MaximumData2 : DbbDbwDbdAddress
            {
                public MaximumData2(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD330";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 330;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class MaxStatusRotation : DbbDbwDbdAddress
            {
                public MaxStatusRotation(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD334";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 334;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class MinStatusRotation : DbbDbwDbdAddress
            {
                public MinStatusRotation(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD338";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 338;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StructureEfficiency : DbbDbwDbdAddress
            {
                public StructureEfficiency(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD342";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 342;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class Compensation : DbbDbwDbdAddress
            {
                public Compensation(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD346";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 346;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class SurplusData1 : DbbDbwDbdAddress
            {
                public SurplusData1(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD350";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 350;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class SurplusData2 : DbbDbwDbdAddress
            {
                public SurplusData2(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD354";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 350;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class SurplusData3 : DbbDbwDbdAddress
            {
                public SurplusData3(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD358";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 358;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class SurplusData4 : DbbDbwDbdAddress
            {
                public SurplusData4(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD362";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 362;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StopTorqueHMI : DbbDbwDbdAddress
            {
                public StopTorqueHMI(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD370";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 370;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class StopInput : DbbDbwDbdAddress
            {
                public StopInput(S7Client client) : base(client)
                {
                }

                public override string Name { get; } = "DB23.DBD374";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 23;
                protected override int Offset { get; } = 374;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }
        }

        public partial class MAddressModel
        {
            public class M20 : BWDAddress
            {
                public M20(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "M20";
                protected override Types Type { get; } = Types.Real;
                protected override int Number { get; } = 20;
                protected override int Pos { get; } = 0;
                protected override int BufferLength { get; } = 4;
            }

            public class M50 : XStringAddress
            {
                public M50(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "M50";
                protected override Types Type { get; } = Types.StringASCII;
                protected override int Number { get; } = 50;
                protected override int Pos { get; } = 0;
                protected override int MaxLength { get; } = 10;
            }

            public class M401_1 : XAddress
            {
                public M401_1(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "M401.1";
                protected override Types Type { get; } = Types.Bit;
                protected override int Number { get; } = 401;
                protected override int Bit { get; } = 1;
                protected override int Pos { get; } = 0;
            }

            public class M401_2 : XAddress
            {
                public M401_2(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "M401.2";
                protected override Types Type { get; } = Types.Bit;
                protected override int Number { get; } = 401;
                protected override int Bit { get; } = 2;
                protected override int Pos { get; } = 0;
            }

            public class M401_3 : XAddress
            {
                public M401_3(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "M401.3";
                protected override Types Type { get; } = Types.Bit;
                protected override int Number { get; } = 401;
                protected override int Bit { get; } = 3;
                protected override int Pos { get; } = 0;
            }

            public class M401_4 : XAddress
            {
                public M401_4(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "M401.4";
                protected override Types Type { get; } = Types.Bit;
                protected override int Number { get; } = 401;
                protected override int Bit { get; } = 4;
                protected override int Pos { get; } = 0;
            }

            public class M401_5 : XAddress
            {
                public M401_5(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "M401.5";
                protected override Types Type { get; } = Types.Bit;
                protected override int Number { get; } = 401;
                protected override int Bit { get; } = 5;
                protected override int Pos { get; } = 0;
            }

            public class M401_6 : XAddress
            {
                public M401_6(S7Client client) : base(client)
                {

                }

                public override string Name { get; } = "M401.6";
                protected override Types Type { get; } = Types.Bit;
                protected override int Number { get; } = 401;
                protected override int Bit { get; } = 6;
                protected override int Pos { get; } = 0;
            }
        }
    }
}
