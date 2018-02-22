namespace JE.RMS.Common.Constants
{
    public class RewardRequestSchema
    {
        #region RewardRequestSchema
        //For All request other than EnergyEarth
        public const string RequestSchema = @"{
                    '$schema': 'http://json-schema.org/draft-04/schema#',
                    'definitions': {},
                    'id': 'http://example.com/example.json',
                    'required': [ 'RequestID', 'TransactionType', 'Reward', 'Customer' ],
                    'properties': {
                        'AdditionalData': {
                            'items': {
                                'properties': {
                                    'Name': {
                                        'type': 'string'
                                    },
                                    'Value': {
                                        'type': 'string'
                                    }
                                },
                                'type': 'object'
                            },
                            'type': 'array'
                        },
                        'Customer': {
                            'required': [ 'SourceSystemUniqueID', 'SourceSystemUniqueIDType', 'FirstName', 'LastName' ],
                            'properties': {
                                'AddressLine1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'AddressLine2': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'City': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'CompanyName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'Email': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100
                                },
                                'FirstName': {
                                    'type': 'string',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'Language': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10,
                                },
                                'LastName': {
                                    'type': 'string',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'MasterID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 20
                                },
                                'Phone1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50
                                },
                                'Product': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 15
                                },
                                'SourceSystemID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 6,
                                },
                                'SourceSystemName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 30,
                                },
                                'SourceSystemUniqueID': {
                                    'type': 'string',
                                    'maxLength': 30,
                                    'minLength': 1
                                },
                                'SourceSystemUniqueIDType': {
                                    'type': 'string',
                                    'maxLength': 30,
                                    'minLength': 1
                                },
                                'StateProvince': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                },
                                'ZipPostalCode': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                }
                            },
                            'type': 'object'
                        },
                        'RequestID': {
                            'type': 'string',
                            'maxLength': 50,
                            'minLength': 1
                        },
                        'Reward': {
                            'required': [ 'ProductCode', 'ProductValue', 'EffectiveDate', 'ProgramName' ],
                            'properties': {
                                'EffectiveDate': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'ProductCode': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'ProductValue': {
                                    'type': 'string',
                                    'minLength': 1,
                                    'maxLength': 8
                                },
                                'ProgramName': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'RewardType': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50
                                }
                            },
                            'type': 'object'
                        },
                        'TransactionType': {
                            'type': 'string',
                            'minLength': 1,
                            'maxLength': 30
                        }
                    },
                    'type': 'object'
                }";

        //For EnergyEarth Reward request
        public const string RewardSchema = @"{
                    '$schema': 'http://json-schema.org/draft-04/schema#',
                    'definitions': {},
                    'id': 'http://example.com/example.json',
                    'required': [ 'RequestID', 'TransactionType', 'Reward', 'Customer' ],
                    'properties': {
                        'AdditionalData': {
                            'items': {
                                'properties': {
                                    'Name': {
                                        'type': 'string'
                                    },
                                    'Value': {
                                        'type': 'string'
                                    }
                                },
                                'type': 'object'
                            },
                            'type': 'array'
                        },
                        'Customer': {
                            'required': [ 'SourceSystemUniqueID', 'SourceSystemUniqueIDType', 'Email', 'FirstName', 'LastName', 'MasterID' ],
                            'properties': {
                                'AddressLine1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'AddressLine2': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'City': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'CompanyName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'Email': {
                                    'type': 'string',
                                    'format':'email',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'FirstName': {
                                    'type': 'string',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'Language': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10,
                                },
                                'LastName': {
                                    'type': 'string',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'MasterID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 20
                                },
                                'Phone1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50
                                },
                                'Product': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 15
                                },
                                'SourceSystemID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 6,
                                },
                                'SourceSystemName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 30,
                                },
                                'SourceSystemUniqueID': {
                                    'type': 'string',
                                    'maxLength': 30,
                                    'minLength': 1
                                },
                                'SourceSystemUniqueIDType': {
                                    'type': 'string',
                                    'maxLength': 30,
                                    'minLength': 1
                                },
                                'StateProvince': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                },
                                'ZipPostalCode': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                }
                            },
                            'type': 'object'
                        },
                        'RequestID': {
                            'type': 'string',
                            'maxLength': 50,
                            'minLength': 1
                        },
                        'Reward': {
                            'required': [ 'ProductCode', 'ProductValue', 'EffectiveDate', 'ProgramName'],
                            'properties': {
                                'EffectiveDate': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'ProductCode': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'ProductValue': {
                                    'type': 'string',
                                    'minLength': 1,
                                    'maxLength': 8
                                },
                                'ProgramName': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'RewardType': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50
                                }
                            },
                            'type': 'object'
                        },
                        'TransactionType': {
                            'type': 'string',
                            'minLength': 1,
                            'maxLength': 30
                        }
                    },
                    'type': 'object'
                }";

        public const string OrderRewardSchema = @"{
                    '$schema': 'http://json-schema.org/draft-04/schema#',
                    'definitions': {},
                    'id': 'http://example.com/example.json',
                    'required': [ 'RequestID', 'TransactionType', 'Reward', 'Customer' ],
                    'properties': {
                        'Customer': {
                            'required': [ 'FirstName', 'LastName' ],
                            'properties': {
                                'AddressLine1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'AddressLine2': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'City': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'CompanyName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'Email': {
                                    'anyOf': [
                                        {'type':'string', 'format':'email', 'maxLength': 100,},
                                        {'type':'string', 'enum':[''], 'maxLength': 100,}]
                                },
                                'FirstName': {
                                    'type': 'string',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'Language': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10,
                                },
                                'LastName': {
                                    'type': 'string',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'MasterID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 20
                                },
                                'Phone1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50
                                },
                                'Product': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 15
                                },
                                'SourceSystemID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 6,
                                },
                                'SourceSystemName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 30,
                                },
                                'SourceSystemUniqueID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 30,
                                },
                                'SourceSystemUniqueIDType': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 30,
                                },
                                'StateProvince': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                },
                                'ZipPostalCode': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                }
                            },
                            'type': 'object'
                        },
                        'RequestID': {
                            'type': 'string',
                            'maxLength': 50,
                            'minLength': 1
                        },
                        'Reward': {
                            'required': [ 'ProductCode', 'ProgramName'],
                            'properties': {
                                'ProductCode': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'ProductValue': {
                                    'type': 'string',
                                    'maxLength': 8
                                },
                                'ProgramName': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'RewardType': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50
                                }
                            },
                            'type': 'object'
                        },
                        'TransactionType': {
                            'type': 'string',
                            'minLength': 1,
                            'maxLength': 30
                        }
                    },
                    'type': 'object'
                }";


        //EnergyEarth transaction type - Terminate, Reactivate, Qualify
        public const string Terminate_Reactivate_Qualify_Schema = @"{
                    '$schema': 'http://json-schema.org/draft-04/schema#',
                    'definitions': {},
                    'id': 'http://example.com/example.json',
                    'required': [ 'RequestID', 'TransactionType', 'Customer' ],
                    'properties': {
                        'AdditionalData': {
                            'items': {
                                'properties': {
                                    'Name': {
                                        'type': 'string'
                                    },
                                    'Value': {
                                        'type': 'string'
                                    }
                                },
                                'type': 'object'
                            },
                            'type': 'array'
                        },
                        'Customer': {
                            'required': [ 'SourceSystemUniqueID', 'SourceSystemUniqueIDType', 'Email'],
                            'properties': {
                                'AddressLine1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'AddressLine2': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'City': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'CompanyName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'Email': {
                                    'type': 'string',
                                    'format':'email',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'FirstName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100
                                },
                                'Language': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                },
                                'LastName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100
                                },
                                'MasterID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 20
                                },
                                'Phone1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'Product': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 15,
                                },
                                'SourceSystemID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 6,
                                },
                                'SourceSystemName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 30,
                                },
                                'SourceSystemUniqueID': {
                                    'type': 'string',
                                    'maxLength': 30,
                                    'minLength': 1
                                },
                                'SourceSystemUniqueIDType': {
                                    'type': 'string',
                                    'maxLength': 30,
                                    'minLength': 1
                                },
                                'StateProvince': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                },
                                'ZipPostalCode': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                }
                            },
                            'type': 'object'
                        },
                        'RequestID': {
                            'type': 'string',
                            'maxLength': 50,
                            'minLength': 1
                        },
                        'TransactionType': {
                            'type': 'string',
                            'minLength': 1,
                            'maxLength': 30
                        }
                    },
                    'type': 'object'
                }";

        public const string ProgramUpdateSchema = @"{
                    '$schema': 'http://json-schema.org/draft-04/schema#',
                    'definitions': {},
                    'id': 'http://example.com/example.json',
                    'required': [ 'RequestID', 'TransactionType', 'Customer', 'Reward' ],
                    'properties': {
                        'AdditionalData': {
                            'items': {
                                'properties': {
                                    'Name': {
                                        'type': 'string'
                                    },
                                    'Value': {
                                        'type': 'string'
                                    }
                                },
                                'type': 'object'
                            },
                            'type': 'array'
                        },
                        'Customer': {
                            'required': [ 'SourceSystemUniqueID', 'SourceSystemUniqueIDType', 'Email', 'FirstName', 'LastName' ],
                            'properties': {
                                'AddressLine1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'AddressLine2': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 100,
                                },
                                'City': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'CompanyName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'Email': {
                                    'type': 'string',
                                    'format':'email',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'FirstName': {
                                    'type': 'string',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'Language': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                },
                                'LastName': {
                                    'type': 'string',
                                    'maxLength': 100,
                                    'minLength': 1
                                },
                                'MasterID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 20
                                },
                                'Phone1': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50,
                                },
                                'Product': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 15,
                                },
                                'SourceSystemID': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 6,
                                },
                                'SourceSystemName': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 30,
                                },
                                'SourceSystemUniqueID': {
                                    'type': 'string',
                                    'maxLength': 30,
                                    'minLength': 1
                                },
                                'SourceSystemUniqueIDType': {
                                    'type': 'string',
                                    'maxLength': 30,
                                    'minLength': 1
                                },
                                'StateProvince': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                },
                                'ZipPostalCode': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 10
                                }
                            },
                            'type': 'object'
                        },
                        'Reward': {
                            'required': [ 'ProgramName'],
                            'properties': {
                                'ProgramName': {
                                    'type': 'string',
                                    'maxLength': 50,
                                    'minLength': 1
                                },
                                'RewardType': {
                                    'type': [ 'string', 'null' ],
                                    'maxLength': 50
                                }
                            },
                            'type': 'object'
                        },
                        'RequestID': {
                            'type': 'string',
                            'maxLength': 50,
                            'minLength': 1
                        },
                        'TransactionType': {
                            'type': 'string',
                            'minLength': 1,
                            'maxLength': 30
                        }
                    },
                    'type': 'object'
                }";

        #endregion

        #region FulfillmentResponseSchema
        public const string FulfillmentResponseSchema = @"{
                                                              '$schema': 'http://json-schema.org/draft-04/schema#',
                                                              'definitions': {},
                                                              'id': 'http://example.com/example.json',
                                                              'required': [ 'Message', 'RMSRewardID', 'RequestId', 'Status' ],
                                                              'properties': {
                                                                  'Message': {
                                                                      'type': 'string',
                                                                      'minLength': 1
                                                                  },
                                                                  'RMSRewardID': {
                                                                      'type': 'string',
                                                                      'minLength': 1
                                                                  },
                                                                  'RequestId': {
                                                                      'type': 'string',
                                                                      'minLength': 1
                                                                  },
                                                                  'Status': {
                                                                      'enum': [ 'Success', 'Fail'],
                                                                      'type': 'string',
                                                                      'minLength': 1
                                                                  }
                                                              },
                                                              'type': 'object'
                                                          }";

        #endregion 
    }
}
