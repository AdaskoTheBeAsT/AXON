package axon;

import java.util.List;

public record ParseResult(List<Schema> schemas, List<DataBlock> dataBlocks) {
}
