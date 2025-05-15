using UnityEngine;
						
namespace ProceduralMeshes {

	public interface IMeshGenerator {
        int VertexCount {get; }

        int IndexCount {get;}

        int JobLength {get;}

		void Execute<S> (int i, S streams) where S : struct, IMeshStreams;
	}
}
