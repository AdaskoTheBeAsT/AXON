import { AxonParser } from './axon-parser.js';

const axonData = `
@schema User
id:I
name:S
email:S
active:B
age:I?
@end

@data User[3]
1|Alice|alice@example.com|1|28
2|Bob|bob@example.com|0|_
3|Carol|carol@example.com|1|35
@end
`;

const { schemas, dataBlocks } = AxonParser.parse(axonData);

console.log(`Parsed ${schemas.length} schema(s) and ${dataBlocks.length} data block(s)\n`);

for (const schema of schemas) {
  console.log(`Schema: ${schema.name}`);
  for (const field of schema.fields) {
    const nullable = field.isNullable ? '?' : '';
    console.log(`  - ${field.name}: ${field.type}${nullable}`);
  }
  console.log();
}

for (const dataBlock of dataBlocks) {
  console.log(`Data: ${dataBlock.schemaName} (${dataBlock.rows.length} rows)`);
  for (const row of dataBlock.rows) {
    const values = Object.entries(row)
      .map(([key, value]) => `${key}=${value ?? 'null'}`)
      .join(' ');
    console.log(`  ${values}`);
  }
  console.log();
}
