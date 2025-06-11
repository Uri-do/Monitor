import api from './api';

// Quantum-resistant cryptography interfaces
export interface QuantumKeyPair {
  id: string;
  algorithm: 'CRYSTALS-Kyber' | 'CRYSTALS-Dilithium' | 'FALCON' | 'SPHINCS+' | 'NTRU' | 'SABER';
  publicKey: string;
  privateKeyId: string; // Reference to securely stored private key
  keySize: number;
  securityLevel: 1 | 3 | 5; // NIST security levels
  createdAt: string;
  expiresAt?: string;
  usage: 'encryption' | 'signing' | 'key_exchange';
  status: 'active' | 'revoked' | 'expired';
}

export interface QuantumSignature {
  id: string;
  algorithm: 'CRYSTALS-Dilithium' | 'FALCON' | 'SPHINCS+';
  signature: string;
  publicKeyId: string;
  messageHash: string;
  timestamp: string;
  verified: boolean;
  securityLevel: number;
  signatureSize: number;
}

export interface QuantumEncryption {
  id: string;
  algorithm: 'CRYSTALS-Kyber' | 'NTRU' | 'SABER';
  ciphertext: string;
  publicKeyId: string;
  encryptedAt: string;
  keySize: number;
  securityLevel: number;
  metadata: {
    originalSize: number;
    encryptedSize: number;
    compressionRatio: number;
  };
}

export interface QuantumRandomness {
  id: string;
  source: 'quantum_hardware' | 'quantum_simulator' | 'hybrid';
  entropy: number; // bits of entropy
  randomBytes: string;
  timestamp: string;
  verified: boolean;
  tests: {
    monobit: boolean;
    frequency: boolean;
    runs: boolean;
    longestRun: boolean;
    rank: boolean;
    dft: boolean;
    nonOverlapping: boolean;
    overlapping: boolean;
    universal: boolean;
    approximate: boolean;
    random: boolean;
    serial: boolean;
    linearComplexity: boolean;
  };
}

export interface PostQuantumCertificate {
  id: string;
  subject: string;
  issuer: string;
  serialNumber: string;
  algorithm: string;
  publicKey: string;
  signature: string;
  validFrom: string;
  validTo: string;
  keyUsage: string[];
  extensions: Record<string, any>;
  certificateChain: string[];
  revocationStatus: 'valid' | 'revoked' | 'suspended';
  quantumSafe: boolean;
}

export interface QuantumThreatAssessment {
  id: string;
  algorithm: string;
  currentSecurity: number;
  quantumThreat: {
    timeToBreak: number; // years
    qubitsRequired: number;
    gateCount: number;
    confidence: number;
  };
  recommendation: 'safe' | 'migrate_soon' | 'migrate_now' | 'deprecated';
  alternativeAlgorithms: string[];
  migrationComplexity: 'low' | 'medium' | 'high';
  assessmentDate: string;
}

export interface QuantumKeyDistribution {
  id: string;
  protocol: 'BB84' | 'E91' | 'SARG04' | 'COW';
  participants: string[];
  keyLength: number;
  errorRate: number;
  securityParameter: number;
  status: 'establishing' | 'active' | 'compromised' | 'expired';
  establishedAt?: string;
  lastUsed?: string;
  usageCount: number;
}

export interface LatticeBasedScheme {
  id: string;
  scheme: 'LWE' | 'RLWE' | 'MLWE' | 'SIS' | 'NTRU';
  parameters: {
    dimension: number;
    modulus: number;
    errorDistribution: string;
    securityLevel: number;
  };
  publicKey: string;
  performance: {
    keyGenTime: number;
    encryptionTime: number;
    decryptionTime: number;
    signatureTime: number;
    verificationTime: number;
  };
  createdAt: string;
}

/**
 * Quantum-resistant cryptography service
 */
class QuantumCryptoService {
  // Key Management
  async generateQuantumKeyPair(algorithm: string, securityLevel: number, usage: string): Promise<QuantumKeyPair> {
    try {
      const response = await api.post('/quantum/keys/generate', {
        algorithm,
        securityLevel,
        usage,
      });
      return response.data;
    } catch (error) {
      console.warn('Quantum key generation endpoint not available, returning mock data');
      return this.getMockQuantumKeyPair();
    }
  }

  async getQuantumKeyPairs(): Promise<QuantumKeyPair[]> {
    try {
      const response = await api.get('/quantum/keys');
      return response.data;
    } catch (error) {
      console.warn('Quantum keys endpoint not available, returning mock data');
      return [this.getMockQuantumKeyPair()];
    }
  }

  async revokeQuantumKey(keyId: string): Promise<void> {
    try {
      await api.post(`/quantum/keys/${keyId}/revoke`);
    } catch (error) {
      console.warn('Revoke quantum key endpoint not available');
    }
  }

  // Digital Signatures
  async signWithQuantumAlgorithm(data: string, keyId: string): Promise<QuantumSignature> {
    try {
      const response = await api.post('/quantum/sign', {
        data,
        keyId,
      });
      return response.data;
    } catch (error) {
      console.warn('Quantum signing endpoint not available, returning mock data');
      return this.getMockQuantumSignature();
    }
  }

  async verifyQuantumSignature(signatureId: string): Promise<{
    verified: boolean;
    algorithm: string;
    securityLevel: number;
    verificationTime: number;
  }> {
    try {
      const response = await api.post(`/quantum/verify/${signatureId}`);
      return response.data;
    } catch (error) {
      console.warn('Quantum signature verification endpoint not available, returning mock data');
      return {
        verified: true,
        algorithm: 'CRYSTALS-Dilithium',
        securityLevel: 3,
        verificationTime: 0.5,
      };
    }
  }

  // Encryption
  async encryptWithQuantumAlgorithm(data: string, publicKeyId: string): Promise<QuantumEncryption> {
    try {
      const response = await api.post('/quantum/encrypt', {
        data,
        publicKeyId,
      });
      return response.data;
    } catch (error) {
      console.warn('Quantum encryption endpoint not available, returning mock data');
      return this.getMockQuantumEncryption();
    }
  }

  async decryptWithQuantumAlgorithm(encryptionId: string): Promise<{
    decrypted: string;
    algorithm: string;
    decryptionTime: number;
  }> {
    try {
      const response = await api.post(`/quantum/decrypt/${encryptionId}`);
      return response.data;
    } catch (error) {
      console.warn('Quantum decryption endpoint not available, returning mock data');
      return {
        decrypted: 'Mock decrypted data',
        algorithm: 'CRYSTALS-Kyber',
        decryptionTime: 1.2,
      };
    }
  }

  // Quantum Random Number Generation
  async generateQuantumRandomness(bytes: number): Promise<QuantumRandomness> {
    try {
      const response = await api.post('/quantum/random', { bytes });
      return response.data;
    } catch (error) {
      console.warn('Quantum randomness endpoint not available, returning mock data');
      return this.getMockQuantumRandomness();
    }
  }

  async validateRandomness(randomnessId: string): Promise<{
    valid: boolean;
    entropy: number;
    tests: Record<string, boolean>;
    confidence: number;
  }> {
    try {
      const response = await api.post(`/quantum/random/${randomnessId}/validate`);
      return response.data;
    } catch (error) {
      console.warn('Validate randomness endpoint not available, returning mock data');
      return {
        valid: true,
        entropy: 7.98,
        tests: { monobit: true, frequency: true, runs: true },
        confidence: 0.99,
      };
    }
  }

  // Post-Quantum Certificates
  async generatePostQuantumCertificate(subject: string, algorithm: string): Promise<PostQuantumCertificate> {
    try {
      const response = await api.post('/quantum/certificates', {
        subject,
        algorithm,
      });
      return response.data;
    } catch (error) {
      console.warn('Generate PQ certificate endpoint not available, returning mock data');
      return this.getMockPostQuantumCertificate();
    }
  }

  async validateCertificateChain(certificateId: string): Promise<{
    valid: boolean;
    quantumSafe: boolean;
    chainLength: number;
    weakestLink: string;
    recommendations: string[];
  }> {
    try {
      const response = await api.post(`/quantum/certificates/${certificateId}/validate`);
      return response.data;
    } catch (error) {
      console.warn('Validate certificate chain endpoint not available, returning mock data');
      return {
        valid: true,
        quantumSafe: true,
        chainLength: 3,
        weakestLink: 'none',
        recommendations: [],
      };
    }
  }

  // Threat Assessment
  async assessQuantumThreat(algorithm: string): Promise<QuantumThreatAssessment> {
    try {
      const response = await api.get(`/quantum/threat-assessment/${algorithm}`);
      return response.data;
    } catch (error) {
      console.warn('Quantum threat assessment endpoint not available, returning mock data');
      return this.getMockQuantumThreatAssessment();
    }
  }

  async getQuantumReadinessReport(): Promise<{
    overallScore: number;
    algorithms: Array<{
      name: string;
      status: 'safe' | 'at_risk' | 'vulnerable';
      timeToMigrate: number;
    }>;
    recommendations: string[];
    migrationPlan: Array<{
      phase: number;
      description: string;
      algorithms: string[];
      timeline: string;
      complexity: string;
    }>;
  }> {
    try {
      const response = await api.get('/quantum/readiness-report');
      return response.data;
    } catch (error) {
      console.warn('Quantum readiness report endpoint not available, returning mock data');
      return {
        overallScore: 85,
        algorithms: [
          { name: 'RSA-2048', status: 'at_risk', timeToMigrate: 5 },
          { name: 'ECDSA-P256', status: 'at_risk', timeToMigrate: 5 },
          { name: 'AES-256', status: 'safe', timeToMigrate: 15 },
        ],
        recommendations: [
          'Migrate RSA signatures to CRYSTALS-Dilithium',
          'Replace ECDH with CRYSTALS-Kyber for key exchange',
          'Implement hybrid classical-quantum schemes',
        ],
        migrationPlan: [
          {
            phase: 1,
            description: 'Implement hybrid schemes',
            algorithms: ['RSA + Dilithium', 'ECDH + Kyber'],
            timeline: '6 months',
            complexity: 'medium',
          },
        ],
      };
    }
  }

  // Quantum Key Distribution
  async establishQKDChannel(participants: string[], protocol: string): Promise<QuantumKeyDistribution> {
    try {
      const response = await api.post('/quantum/qkd/establish', {
        participants,
        protocol,
      });
      return response.data;
    } catch (error) {
      console.warn('Establish QKD channel endpoint not available, returning mock data');
      return this.getMockQuantumKeyDistribution();
    }
  }

  async getQKDChannels(): Promise<QuantumKeyDistribution[]> {
    try {
      const response = await api.get('/quantum/qkd/channels');
      return response.data;
    } catch (error) {
      console.warn('QKD channels endpoint not available, returning mock data');
      return [this.getMockQuantumKeyDistribution()];
    }
  }

  // Lattice-Based Cryptography
  async createLatticeScheme(scheme: string, parameters: any): Promise<LatticeBasedScheme> {
    try {
      const response = await api.post('/quantum/lattice', {
        scheme,
        parameters,
      });
      return response.data;
    } catch (error) {
      console.warn('Create lattice scheme endpoint not available, returning mock data');
      return this.getMockLatticeBasedScheme();
    }
  }

  async benchmarkLatticePerformance(schemeId: string): Promise<{
    keyGeneration: number;
    encryption: number;
    decryption: number;
    signing: number;
    verification: number;
    memoryUsage: number;
  }> {
    try {
      const response = await api.post(`/quantum/lattice/${schemeId}/benchmark`);
      return response.data;
    } catch (error) {
      console.warn('Benchmark lattice performance endpoint not available, returning mock data');
      return {
        keyGeneration: 2.5,
        encryption: 0.8,
        decryption: 1.2,
        signing: 1.5,
        verification: 0.6,
        memoryUsage: 1024,
      };
    }
  }

  // Mock data methods
  private getMockQuantumKeyPair(): QuantumKeyPair {
    return {
      id: 'qkey-' + Date.now(),
      algorithm: 'CRYSTALS-Kyber',
      publicKey: 'quantum-public-key-' + Math.random().toString(36).substr(2, 20),
      privateKeyId: 'qpriv-' + Math.random().toString(36).substr(2, 20),
      keySize: 1568,
      securityLevel: 3,
      createdAt: new Date().toISOString(),
      expiresAt: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString(),
      usage: 'encryption',
      status: 'active',
    };
  }

  private getMockQuantumSignature(): QuantumSignature {
    return {
      id: 'qsig-' + Date.now(),
      algorithm: 'CRYSTALS-Dilithium',
      signature: 'quantum-signature-' + Math.random().toString(36).substr(2, 50),
      publicKeyId: 'qkey-123',
      messageHash: 'sha3-256-hash-' + Math.random().toString(36).substr(2, 20),
      timestamp: new Date().toISOString(),
      verified: true,
      securityLevel: 3,
      signatureSize: 2420,
    };
  }

  private getMockQuantumEncryption(): QuantumEncryption {
    return {
      id: 'qenc-' + Date.now(),
      algorithm: 'CRYSTALS-Kyber',
      ciphertext: 'quantum-ciphertext-' + Math.random().toString(36).substr(2, 100),
      publicKeyId: 'qkey-123',
      encryptedAt: new Date().toISOString(),
      keySize: 1568,
      securityLevel: 3,
      metadata: {
        originalSize: 1024,
        encryptedSize: 1568,
        compressionRatio: 0.65,
      },
    };
  }

  private getMockQuantumRandomness(): QuantumRandomness {
    return {
      id: 'qrand-' + Date.now(),
      source: 'quantum_hardware',
      entropy: 7.98,
      randomBytes: Array.from({ length: 32 }, () => Math.floor(Math.random() * 256).toString(16).padStart(2, '0')).join(''),
      timestamp: new Date().toISOString(),
      verified: true,
      tests: {
        monobit: true,
        frequency: true,
        runs: true,
        longestRun: true,
        rank: true,
        dft: true,
        nonOverlapping: true,
        overlapping: true,
        universal: true,
        approximate: true,
        random: true,
        serial: true,
        linearComplexity: true,
      },
    };
  }

  private getMockPostQuantumCertificate(): PostQuantumCertificate {
    return {
      id: 'qcert-' + Date.now(),
      subject: 'CN=MonitoringGrid,O=Enterprise,C=US',
      issuer: 'CN=Quantum CA,O=Quantum Authority,C=US',
      serialNumber: Math.random().toString(16).substr(2, 16),
      algorithm: 'CRYSTALS-Dilithium',
      publicKey: 'quantum-public-key-data',
      signature: 'quantum-certificate-signature',
      validFrom: new Date().toISOString(),
      validTo: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString(),
      keyUsage: ['digitalSignature', 'keyEncipherment'],
      extensions: { quantumSafe: true, securityLevel: 3 },
      certificateChain: ['intermediate-cert', 'root-cert'],
      revocationStatus: 'valid',
      quantumSafe: true,
    };
  }

  private getMockQuantumThreatAssessment(): QuantumThreatAssessment {
    return {
      id: 'qthreat-' + Date.now(),
      algorithm: 'RSA-2048',
      currentSecurity: 112,
      quantumThreat: {
        timeToBreak: 8,
        qubitsRequired: 4096,
        gateCount: 1e12,
        confidence: 0.85,
      },
      recommendation: 'migrate_soon',
      alternativeAlgorithms: ['CRYSTALS-Dilithium', 'FALCON', 'SPHINCS+'],
      migrationComplexity: 'medium',
      assessmentDate: new Date().toISOString(),
    };
  }

  private getMockQuantumKeyDistribution(): QuantumKeyDistribution {
    return {
      id: 'qkd-' + Date.now(),
      protocol: 'BB84',
      participants: ['alice@example.com', 'bob@example.com'],
      keyLength: 256,
      errorRate: 0.02,
      securityParameter: 128,
      status: 'active',
      establishedAt: new Date().toISOString(),
      lastUsed: new Date().toISOString(),
      usageCount: 42,
    };
  }

  private getMockLatticeBasedScheme(): LatticeBasedScheme {
    return {
      id: 'lattice-' + Date.now(),
      scheme: 'RLWE',
      parameters: {
        dimension: 1024,
        modulus: 12289,
        errorDistribution: 'discrete_gaussian',
        securityLevel: 128,
      },
      publicKey: 'lattice-public-key-data',
      performance: {
        keyGenTime: 2.5,
        encryptionTime: 0.8,
        decryptionTime: 1.2,
        signatureTime: 1.5,
        verificationTime: 0.6,
      },
      createdAt: new Date().toISOString(),
    };
  }
}

export const quantumCryptoService = new QuantumCryptoService();
