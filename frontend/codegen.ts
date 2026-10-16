import type { CodegenConfig } from '@graphql-codegen/cli';

/**
 * Generates TypeScript types and typed document nodes for every operation in src/**\/*.graphql from the
 * gateway's committed schema snapshot. Apollo Client infers result and variable types from the documents.
 */
const config: CodegenConfig = {
  schema: '../backend/src/Services/Gateway/Telemetry.Gateway.Api/schema.graphql',
  documents: 'src/app/**/*.graphql',
  generates: {
    'src/app/core/graphql/generated.ts': {
      plugins: ['typescript-operations', 'typed-document-node'],
      config: {
        enumsAsTypes: true,
        useTypeImports: true,
        strictScalars: true,
        scalars: {
          DateTime: 'string',
          Long: 'number',
        },
        avoidOptionals: { field: true, inputValue: false, object: true, defaultValue: false },
      },
    },
  },
};

export default config;
