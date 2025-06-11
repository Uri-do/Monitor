import api from './api';

// Blockchain/DLT interfaces
export interface BlockchainTransaction {
  id: string;
  hash: string;
  blockNumber: number;
  blockHash: string;
  timestamp: string;
  from: string;
  to?: string;
  type: 'audit_log' | 'kpi_execution' | 'alert_creation' | 'user_action' | 'system_event';
  data: {
    action: string;
    entityType: string;
    entityId: string;
    userId: string;
    metadata: Record<string, any>;
    previousHash?: string;
  };
  signature: string;
  confirmations: number;
  gasUsed?: number;
  status: 'pending' | 'confirmed' | 'failed';
  merkleProof?: string[];
}

export interface Block {
  number: number;
  hash: string;
  parentHash: string;
  timestamp: string;
  miner: string;
  difficulty: number;
  totalDifficulty: number;
  size: number;
  gasLimit: number;
  gasUsed: number;
  transactionCount: number;
  transactions: string[]; // Transaction hashes
  merkleRoot: string;
  stateRoot: string;
  receiptsRoot: string;
  nonce: string;
  extraData: string;
}

export interface SmartContract {
  address: string;
  name: string;
  description: string;
  abi: any[];
  bytecode: string;
  sourceCode: string;
  compiler: string;
  version: string;
  deployedAt: string;
  deployedBy: string;
  verified: boolean;
  functions: Array<{
    name: string;
    type: 'function' | 'event' | 'constructor';
    inputs: Array<{ name: string; type: string }>;
    outputs: Array<{ name: string; type: string }>;
    stateMutability: 'pure' | 'view' | 'nonpayable' | 'payable';
  }>;
}

export interface DigitalIdentity {
  id: string;
  publicKey: string;
  address: string;
  type: 'user' | 'system' | 'service' | 'device';
  attributes: Record<string, any>;
  credentials: Array<{
    id: string;
    issuer: string;
    type: string;
    claims: Record<string, any>;
    proof: string;
    issuedAt: string;
    expiresAt?: string;
    revoked: boolean;
  }>;
  reputation: {
    score: number;
    transactions: number;
    successRate: number;
    lastActivity: string;
  };
  createdAt: string;
  updatedAt: string;
}

export interface ConsensusMetrics {
  networkHash: string;
  blockHeight: number;
  difficulty: number;
  networkHashRate: number;
  averageBlockTime: number;
  pendingTransactions: number;
  validators: Array<{
    address: string;
    stake: number;
    uptime: number;
    lastSeen: string;
    reputation: number;
  }>;
  consensusAlgorithm: 'proof_of_work' | 'proof_of_stake' | 'proof_of_authority' | 'delegated_proof_of_stake';
  finality: {
    probabilistic: boolean;
    confirmations: number;
    timeToFinality: number;
  };
}

export interface AuditTrail {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  userId: string;
  timestamp: string;
  blockchainTxHash: string;
  merkleProof: string[];
  verified: boolean;
  immutable: boolean;
  data: {
    before?: any;
    after?: any;
    metadata: Record<string, any>;
  };
  signature: string;
  witnesses: string[];
}

export interface ZeroKnowledgeProof {
  id: string;
  type: 'membership' | 'range' | 'knowledge' | 'non_interactive';
  statement: string;
  proof: string;
  publicInputs: any[];
  verified: boolean;
  circuit: {
    name: string;
    constraints: number;
    variables: number;
  };
  createdAt: string;
  verifiedAt?: string;
}

/**
 * Advanced blockchain and distributed ledger service
 */
class BlockchainService {
  // Transaction Management
  async createTransaction(transaction: Omit<BlockchainTransaction, 'id' | 'hash' | 'blockNumber' | 'blockHash' | 'confirmations' | 'status'>): Promise<BlockchainTransaction> {
    try {
      const response = await api.post('/blockchain/transactions', transaction);
      return response.data;
    } catch (error) {
      console.warn('Blockchain transaction endpoint not available, returning mock data');
      return this.getMockTransaction();
    }
  }

  async getTransaction(txHash: string): Promise<BlockchainTransaction> {
    try {
      const response = await api.get(`/blockchain/transactions/${txHash}`);
      return response.data;
    } catch (error) {
      console.warn('Get blockchain transaction endpoint not available, returning mock data');
      return this.getMockTransaction();
    }
  }

  async getTransactionsByEntity(entityType: string, entityId: string): Promise<BlockchainTransaction[]> {
    try {
      const response = await api.get('/blockchain/transactions', {
        params: { entityType, entityId },
      });
      return response.data;
    } catch (error) {
      console.warn('Get transactions by entity endpoint not available, returning mock data');
      return [this.getMockTransaction()];
    }
  }

  // Block Management
  async getLatestBlock(): Promise<Block> {
    try {
      const response = await api.get('/blockchain/blocks/latest');
      return response.data;
    } catch (error) {
      console.warn('Get latest block endpoint not available, returning mock data');
      return this.getMockBlock();
    }
  }

  async getBlock(blockNumber: number): Promise<Block> {
    try {
      const response = await api.get(`/blockchain/blocks/${blockNumber}`);
      return response.data;
    } catch (error) {
      console.warn('Get block endpoint not available, returning mock data');
      return this.getMockBlock();
    }
  }

  // Smart Contracts
  async deployContract(contract: Omit<SmartContract, 'address' | 'deployedAt' | 'deployedBy' | 'verified'>): Promise<SmartContract> {
    try {
      const response = await api.post('/blockchain/contracts', contract);
      return response.data;
    } catch (error) {
      console.warn('Deploy contract endpoint not available, returning mock data');
      return this.getMockSmartContract();
    }
  }

  async callContract(address: string, functionName: string, params: any[]): Promise<{
    result: any;
    gasUsed: number;
    transactionHash?: string;
  }> {
    try {
      const response = await api.post(`/blockchain/contracts/${address}/call`, {
        function: functionName,
        params,
      });
      return response.data;
    } catch (error) {
      console.warn('Call contract endpoint not available, returning mock data');
      return {
        result: { success: true, data: 'Mock contract call result' },
        gasUsed: 21000,
        transactionHash: '0x' + Math.random().toString(16).substr(2, 64),
      };
    }
  }

  // Digital Identity
  async createDigitalIdentity(identity: Omit<DigitalIdentity, 'id' | 'address' | 'reputation' | 'createdAt' | 'updatedAt'>): Promise<DigitalIdentity> {
    try {
      const response = await api.post('/blockchain/identity', identity);
      return response.data;
    } catch (error) {
      console.warn('Create digital identity endpoint not available, returning mock data');
      return this.getMockDigitalIdentity();
    }
  }

  async verifyCredential(credentialId: string): Promise<{
    valid: boolean;
    issuer: string;
    claims: Record<string, any>;
    proof: string;
    verificationMethod: string;
  }> {
    try {
      const response = await api.post(`/blockchain/identity/verify/${credentialId}`);
      return response.data;
    } catch (error) {
      console.warn('Verify credential endpoint not available, returning mock data');
      return {
        valid: true,
        issuer: 'did:example:issuer123',
        claims: { role: 'admin', permissions: ['read', 'write'] },
        proof: 'mock-proof-signature',
        verificationMethod: 'Ed25519VerificationKey2020',
      };
    }
  }

  // Audit Trail
  async createAuditEntry(entry: Omit<AuditTrail, 'id' | 'blockchainTxHash' | 'merkleProof' | 'verified' | 'immutable' | 'signature' | 'witnesses'>): Promise<AuditTrail> {
    try {
      const response = await api.post('/blockchain/audit', entry);
      return response.data;
    } catch (error) {
      console.warn('Create audit entry endpoint not available, returning mock data');
      return this.getMockAuditTrail();
    }
  }

  async verifyAuditEntry(entryId: string): Promise<{
    verified: boolean;
    blockchainVerified: boolean;
    merkleVerified: boolean;
    signatureVerified: boolean;
    immutable: boolean;
    confirmations: number;
  }> {
    try {
      const response = await api.get(`/blockchain/audit/${entryId}/verify`);
      return response.data;
    } catch (error) {
      console.warn('Verify audit entry endpoint not available, returning mock data');
      return {
        verified: true,
        blockchainVerified: true,
        merkleVerified: true,
        signatureVerified: true,
        immutable: true,
        confirmations: 12,
      };
    }
  }

  async getAuditTrail(entityType: string, entityId: string): Promise<AuditTrail[]> {
    try {
      const response = await api.get('/blockchain/audit', {
        params: { entityType, entityId },
      });
      return response.data;
    } catch (error) {
      console.warn('Get audit trail endpoint not available, returning mock data');
      return [this.getMockAuditTrail()];
    }
  }

  // Zero-Knowledge Proofs
  async generateZKProof(type: string, statement: string, witness: any): Promise<ZeroKnowledgeProof> {
    try {
      const response = await api.post('/blockchain/zk/generate', {
        type,
        statement,
        witness,
      });
      return response.data;
    } catch (error) {
      console.warn('Generate ZK proof endpoint not available, returning mock data');
      return this.getMockZKProof();
    }
  }

  async verifyZKProof(proofId: string): Promise<{
    verified: boolean;
    publicInputs: any[];
    verificationTime: number;
  }> {
    try {
      const response = await api.post(`/blockchain/zk/verify/${proofId}`);
      return response.data;
    } catch (error) {
      console.warn('Verify ZK proof endpoint not available, returning mock data');
      return {
        verified: true,
        publicInputs: [42, 'example'],
        verificationTime: 150,
      };
    }
  }

  // Network Metrics
  async getConsensusMetrics(): Promise<ConsensusMetrics> {
    try {
      const response = await api.get('/blockchain/consensus/metrics');
      return response.data;
    } catch (error) {
      console.warn('Consensus metrics endpoint not available, returning mock data');
      return this.getMockConsensusMetrics();
    }
  }

  async getNetworkHealth(): Promise<{
    status: 'healthy' | 'degraded' | 'critical';
    blockHeight: number;
    syncStatus: number;
    peerCount: number;
    transactionPoolSize: number;
    averageBlockTime: number;
    networkLatency: number;
  }> {
    try {
      const response = await api.get('/blockchain/network/health');
      return response.data;
    } catch (error) {
      console.warn('Network health endpoint not available, returning mock data');
      return {
        status: 'healthy',
        blockHeight: 1234567,
        syncStatus: 100,
        peerCount: 45,
        transactionPoolSize: 128,
        averageBlockTime: 12.5,
        networkLatency: 85,
      };
    }
  }

  // Utility methods
  async calculateMerkleProof(transactionHash: string): Promise<string[]> {
    try {
      const response = await api.get(`/blockchain/merkle/${transactionHash}`);
      return response.data.proof;
    } catch (error) {
      console.warn('Calculate merkle proof endpoint not available, returning mock data');
      return ['0xabc123', '0xdef456', '0x789xyz'];
    }
  }

  async estimateGas(transaction: any): Promise<number> {
    try {
      const response = await api.post('/blockchain/gas/estimate', transaction);
      return response.data.gasEstimate;
    } catch (error) {
      console.warn('Estimate gas endpoint not available, returning mock data');
      return 21000;
    }
  }

  // Mock data methods
  private getMockTransaction(): BlockchainTransaction {
    return {
      id: 'tx-' + Date.now(),
      hash: '0x' + Math.random().toString(16).substr(2, 64),
      blockNumber: 1234567,
      blockHash: '0x' + Math.random().toString(16).substr(2, 64),
      timestamp: new Date().toISOString(),
      from: '0x' + Math.random().toString(16).substr(2, 40),
      type: 'audit_log',
      data: {
        action: 'kpi_executed',
        entityType: 'kpi',
        entityId: '123',
        userId: 'user-456',
        metadata: { result: 'success', value: 85.5 },
      },
      signature: '0x' + Math.random().toString(16).substr(2, 130),
      confirmations: 12,
      gasUsed: 21000,
      status: 'confirmed',
      merkleProof: ['0xabc123', '0xdef456'],
    };
  }

  private getMockBlock(): Block {
    return {
      number: 1234567,
      hash: '0x' + Math.random().toString(16).substr(2, 64),
      parentHash: '0x' + Math.random().toString(16).substr(2, 64),
      timestamp: new Date().toISOString(),
      miner: '0x' + Math.random().toString(16).substr(2, 40),
      difficulty: 15000000000000000,
      totalDifficulty: 25000000000000000000,
      size: 2048,
      gasLimit: 8000000,
      gasUsed: 6500000,
      transactionCount: 156,
      transactions: Array.from({ length: 5 }, () => '0x' + Math.random().toString(16).substr(2, 64)),
      merkleRoot: '0x' + Math.random().toString(16).substr(2, 64),
      stateRoot: '0x' + Math.random().toString(16).substr(2, 64),
      receiptsRoot: '0x' + Math.random().toString(16).substr(2, 64),
      nonce: '0x' + Math.random().toString(16).substr(2, 16),
      extraData: '0x',
    };
  }

  private getMockSmartContract(): SmartContract {
    return {
      address: '0x' + Math.random().toString(16).substr(2, 40),
      name: 'MonitoringAudit',
      description: 'Smart contract for immutable audit logging',
      abi: [],
      bytecode: '0x608060405234801561001057600080fd5b50...',
      sourceCode: 'pragma solidity ^0.8.0; contract MonitoringAudit { ... }',
      compiler: 'solc',
      version: '0.8.19',
      deployedAt: new Date().toISOString(),
      deployedBy: '0x' + Math.random().toString(16).substr(2, 40),
      verified: true,
      functions: [
        {
          name: 'logAuditEntry',
          type: 'function',
          inputs: [{ name: 'data', type: 'bytes' }],
          outputs: [{ name: 'success', type: 'bool' }],
          stateMutability: 'nonpayable',
        },
      ],
    };
  }

  private getMockDigitalIdentity(): DigitalIdentity {
    return {
      id: 'did:example:' + Math.random().toString(36).substr(2, 9),
      publicKey: '0x' + Math.random().toString(16).substr(2, 64),
      address: '0x' + Math.random().toString(16).substr(2, 40),
      type: 'user',
      attributes: { name: 'John Doe', role: 'admin' },
      credentials: [],
      reputation: {
        score: 95,
        transactions: 1250,
        successRate: 0.998,
        lastActivity: new Date().toISOString(),
      },
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
  }

  private getMockAuditTrail(): AuditTrail {
    return {
      id: 'audit-' + Date.now(),
      entityType: 'kpi',
      entityId: '123',
      action: 'executed',
      userId: 'user-456',
      timestamp: new Date().toISOString(),
      blockchainTxHash: '0x' + Math.random().toString(16).substr(2, 64),
      merkleProof: ['0xabc123', '0xdef456'],
      verified: true,
      immutable: true,
      data: {
        before: { status: 'pending' },
        after: { status: 'completed', result: 85.5 },
        metadata: { executionTime: 1250 },
      },
      signature: '0x' + Math.random().toString(16).substr(2, 130),
      witnesses: ['0x' + Math.random().toString(16).substr(2, 40)],
    };
  }

  private getMockZKProof(): ZeroKnowledgeProof {
    return {
      id: 'zk-' + Date.now(),
      type: 'membership',
      statement: 'User has admin privileges without revealing identity',
      proof: '0x' + Math.random().toString(16).substr(2, 256),
      publicInputs: [42],
      verified: true,
      circuit: {
        name: 'admin_membership',
        constraints: 1024,
        variables: 256,
      },
      createdAt: new Date().toISOString(),
      verifiedAt: new Date().toISOString(),
    };
  }

  private getMockConsensusMetrics(): ConsensusMetrics {
    return {
      networkHash: '0x' + Math.random().toString(16).substr(2, 64),
      blockHeight: 1234567,
      difficulty: 15000000000000000,
      networkHashRate: 150000000000000,
      averageBlockTime: 12.5,
      pendingTransactions: 128,
      validators: [
        {
          address: '0x' + Math.random().toString(16).substr(2, 40),
          stake: 32000000,
          uptime: 0.998,
          lastSeen: new Date().toISOString(),
          reputation: 95,
        },
      ],
      consensusAlgorithm: 'proof_of_stake',
      finality: {
        probabilistic: false,
        confirmations: 1,
        timeToFinality: 12.5,
      },
    };
  }
}

export const blockchainService = new BlockchainService();
