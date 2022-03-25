namespace NoFS.DayLight.Sovereign {
   public class MeshHelper {
      public MeshHelper(SvrnMaster master) {
         manager = master.getMeshManagerInstance();
      }

      public readonly MeshManager manager;
   }
}