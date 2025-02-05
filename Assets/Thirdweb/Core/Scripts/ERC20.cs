using System;
using System.Threading.Tasks;
using System.Numerics;
using UnityEngine;
using System.Collections.Generic;
using TokenERC20Contract = Thirdweb.Contracts.TokenERC20.ContractDefinition;
using DropERC20Contract = Thirdweb.Contracts.DropERC20.ContractDefinition;

namespace Thirdweb
{
    /// <summary>
    /// Interact with any ERC20 compatible contract.
    /// </summary>
    public class ERC20 : Routable
    {
        /// <summary>
        /// Handle signature minting functionality
        /// </summary>
        public ERC20Signature signature;

        /// <summary>
        /// Query claim conditions
        /// </summary>
        public ERC20ClaimConditions claimConditions;

        private readonly string contractAddress;

        /// <summary>
        /// Interact with any ERC20 compatible contract.
        /// </summary>
        public ERC20(string parentRoute, string contractAddress)
            : base(Routable.append(parentRoute, "erc20"))
        {
            this.contractAddress = contractAddress;
            this.signature = new ERC20Signature(baseRoute, contractAddress);
            this.claimConditions = new ERC20ClaimConditions(baseRoute, contractAddress);
        }

        // READ FUNCTIONS

        /// <summary>
        /// Get the currency information
        /// </summary>
        public async Task<Currency> Get()
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<Currency>(getRoute("get"), new string[] { });
            }
            else
            {
                var decimals = await TransactionManager.ThirdwebRead<TokenERC20Contract.DecimalsFunction, TokenERC20Contract.DecimalsOutputDTO>(
                    contractAddress,
                    new TokenERC20Contract.DecimalsFunction()
                );

                var name = await TransactionManager.ThirdwebRead<TokenERC20Contract.NameFunction, TokenERC20Contract.NameOutputDTO>(contractAddress, new TokenERC20Contract.NameFunction());

                var symbol = await TransactionManager.ThirdwebRead<TokenERC20Contract.SymbolFunction, TokenERC20Contract.SymbolOutputDTO>(contractAddress, new TokenERC20Contract.SymbolFunction());

                return new Currency(name.ReturnValue1, symbol.ReturnValue1, decimals.ReturnValue1.ToString());
            }
        }

        /// <summary>
        /// Get the balance of the connected wallet
        /// </summary>
        public async Task<CurrencyValue> Balance()
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<CurrencyValue>(getRoute("balance"), new string[] { });
            }
            else
            {
                return await BalanceOf(await ThirdwebManager.Instance.SDK.wallet.GetAddress());
            }
        }

        /// <summary>
        /// Get the balance of the specified wallet
        /// </summary>
        public async Task<CurrencyValue> BalanceOf(string address)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<CurrencyValue>(getRoute("balanceOf"), Utils.ToJsonStringArray(address));
            }
            else
            {
                Currency c = await Get();
                var balance = await TransactionManager.ThirdwebRead<TokenERC20Contract.BalanceOfFunction, TokenERC20Contract.BalanceOfOutputDTO>(
                    contractAddress,
                    new TokenERC20Contract.BalanceOfFunction() { Account = address }
                );
                return new CurrencyValue(c.name, c.symbol, c.decimals, balance.ReturnValue1.ToString(), balance.ReturnValue1.ToString().FormatERC20(4, int.Parse(c.decimals), true));
            }
        }

        /// <summary>
        /// Get how much allowance the given address is allowed to spend on behalf of the connected wallet
        /// </summary>
        public async Task<CurrencyValue> Allowance(string spender)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<CurrencyValue>(getRoute("allowance"), Utils.ToJsonStringArray(spender));
            }
            else
            {
                return await AllowanceOf(await ThirdwebManager.Instance.SDK.wallet.GetAddress(), spender);
            }
        }

        /// <summary>
        /// Get how much allowance the given address is allowed to spend on behalf of the specified wallet
        /// </summary>
        public async Task<CurrencyValue> AllowanceOf(string owner, string spender)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<CurrencyValue>(getRoute("allowanceOf"), Utils.ToJsonStringArray(owner, spender));
            }
            else
            {
                Currency c = await Get();
                var allowance = await TransactionManager.ThirdwebRead<TokenERC20Contract.AllowanceFunction, TokenERC20Contract.AllowanceOutputDTO>(
                    contractAddress,
                    new TokenERC20Contract.AllowanceFunction() { Owner = owner, Spender = spender }
                );
                return new CurrencyValue(c.name, c.symbol, c.decimals, allowance.ReturnValue1.ToString(), allowance.ReturnValue1.ToString().FormatERC20(4, int.Parse(c.decimals), true));
            }
        }

        /// <summary>
        /// Get the total supply in circulation
        /// </summary>
        public async Task<CurrencyValue> TotalSupply()
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<CurrencyValue>(getRoute("totalSupply"), new string[] { });
            }
            else
            {
                Currency c = await Get();
                var totalSupply = await TransactionManager.ThirdwebRead<TokenERC20Contract.TotalSupplyFunction, TokenERC20Contract.TotalSupplyOutputDTO>(
                    contractAddress,
                    new TokenERC20Contract.TotalSupplyFunction() { }
                );
                return new CurrencyValue(c.name, c.symbol, c.decimals, totalSupply.ReturnValue1.ToString(), totalSupply.ReturnValue1.ToString().FormatERC20(4, int.Parse(c.decimals), true));
            }
        }

        // WRITE FUNCTIONS

        /// <summary>
        /// Set how much allowance the given address is allowed to spend on behalf of the connected wallet
        /// </summary>
        public async Task<TransactionResult> SetAllowance(string spender, string amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("setAllowance"), Utils.ToJsonStringArray(spender, amount));
            }
            else
            {
                return await TransactionManager.ThirdwebWrite(contractAddress, new TokenERC20Contract.ApproveFunction() { Spender = spender, Amount = BigInteger.Parse(amount.ToWei()) });
            }
        }

        /// <summary>
        /// Transfer a given amount of currency to another wallet
        /// </summary>
        public async Task<TransactionResult> Transfer(string to, string amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("transfer"), Utils.ToJsonStringArray(to, amount));
            }
            else
            {
                var currency = await Get();
                var rawAmountToTransfer = BigInteger.Parse(amount.ToWei()).AdjustDecimals(18, int.Parse(currency.decimals));
                return await TransactionManager.ThirdwebWrite(contractAddress, new TokenERC20Contract.TransferFunction() { To = to, Amount = rawAmountToTransfer });
            }
        }

        /// <summary>
        /// Transfer a given amount of currency from the given wallet (if permission is granted) to another wallet
        /// </summary>
        public async Task<TransactionResult> TransferFrom(string from, string to, string amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("transferFrom"), Utils.ToJsonStringArray(from, to, amount));
            }
            else
            {
                var currency = await Get();
                var rawAmountToTransfer = BigInteger.Parse(amount.ToWei()).AdjustDecimals(18, int.Parse(currency.decimals));
                return await TransactionManager.ThirdwebWrite(
                    contractAddress,
                    new TokenERC20Contract.TransferFromFunction()
                    {
                        From = from,
                        To = to,
                        Amount = rawAmountToTransfer
                    }
                );
            }
        }

        /// <summary>
        /// Burn a given amount of currency
        /// </summary>
        public async Task<TransactionResult> Burn(string amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("burn"), Utils.ToJsonStringArray(amount));
            }
            else
            {
                return await TransactionManager.ThirdwebWrite(contractAddress, new TokenERC20Contract.BurnFunction() { Amount = BigInteger.Parse(amount.ToWei()) });
            }
        }

        /// <summary>
        /// Claim a given amount of currency for compatible drop contracts
        /// </summary>
        public async Task<TransactionResult> Claim(string amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("claim"), Utils.ToJsonStringArray(amount));
            }
            else
            {
                return await ClaimTo(await ThirdwebManager.Instance.SDK.wallet.GetAddress(), amount);
            }
        }

        /// <summary>
        /// Claim a given amount of currency to a given destination wallet for compatible drop contracts
        /// </summary>
        public async Task<TransactionResult> ClaimTo(string address, string amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("claimTo"), Utils.ToJsonStringArray(address, amount));
            }
            else
            {
                var claimCondition = await claimConditions.GetActive();
                var decimals = await TransactionManager.ThirdwebRead<TokenERC20Contract.DecimalsFunction, TokenERC20Contract.DecimalsOutputDTO>(
                    contractAddress,
                    new TokenERC20Contract.DecimalsFunction()
                );
                var rawAmountToClaim = BigInteger.Parse(amount.ToWei()).AdjustDecimals(18, int.Parse(decimals.ReturnValue1.ToString()));
                var rawPrice = BigInteger.Parse(claimCondition.currencyMetadata.value);
                return await TransactionManager.ThirdwebWrite(
                    contractAddress,
                    new DropERC20Contract.ClaimFunction()
                    {
                        Receiver = address,
                        Quantity = rawAmountToClaim,
                        Currency = claimCondition.currencyAddress,
                        PricePerToken = rawPrice,
                        AllowlistProof = new DropERC20Contract.AllowlistProof
                        {
                            Proof = new List<byte[]>(),
                            Currency = claimCondition.currencyAddress,
                            PricePerToken = rawPrice,
                            QuantityLimitPerWallet = BigInteger.Parse(claimCondition.maxClaimablePerWallet),
                        },
                        Data = new byte[] { }
                    },
                    claimCondition.currencyAddress == Utils.NativeTokenAddress ? BigInteger.Parse((decimal.Parse(amount) * (decimal)rawPrice).ToString("F0")) : 0
                );
            }
        }

        /// <summary>
        /// Mint a given amount of currency
        /// </summary>
        public async Task<TransactionResult> Mint(string amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("mint"), Utils.ToJsonStringArray(amount));
            }
            else
            {
                return await MintTo(await ThirdwebManager.Instance.SDK.wallet.GetAddress(), amount);
            }
        }

        /// <summary>
        /// Mint a given amount of currency to a given destination wallet
        /// </summary>
        public async Task<TransactionResult> MintTo(string address, string amount)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("mintTo"), Utils.ToJsonStringArray(address, amount));
            }
            else
            {
                return await TransactionManager.ThirdwebWrite(contractAddress, new TokenERC20Contract.MintToFunction() { To = address, Amount = BigInteger.Parse(amount.ToWei()) });
            }
        }
    }

    [System.Serializable]
    public class ERC20MintPayload
    {
        public string to;
        public string price;
        public string currencyAddress;
        public string primarySaleRecipient;
        public string quantity;
        public string uid;
        public long mintStartTime;
        public long mintEndTime;

        public ERC20MintPayload(string receiverAddress, string quantity)
        {
            this.to = receiverAddress;
            this.quantity = quantity;
            this.price = "0";
            this.currencyAddress = Utils.AddressZero;
            this.primarySaleRecipient = Utils.AddressZero;
            this.uid = Utils.ToBytes32HexString(Guid.NewGuid().ToByteArray());
            this.mintStartTime = Utils.GetUnixTimeStampNow() / 2;
            this.mintEndTime = Utils.GetUnixTimeStampIn10Years();
        }
    }

    [System.Serializable]
    public struct ERC20SignedPayloadOutput
    {
        public string to;
        public string price;
        public string currencyAddress;
        public string primarySaleRecipient;
        public string quantity;
        public string uid;
        public long mintStartTime;
        public long mintEndTime;
    }

    [System.Serializable]
    public struct ERC20SignedPayload
    {
        public string signature;
        public ERC20SignedPayloadOutput payload;
    }

    /// <summary>
    /// Fetch claim conditions for a given ERC20 drop contract
    /// </summary>
#nullable enable
    public class ERC20ClaimConditions : Routable
    {
        private readonly string contractAddress;

        public ERC20ClaimConditions(string parentRoute, string contractAddress)
            : base(Routable.append(parentRoute, "claimConditions"))
        {
            this.contractAddress = contractAddress;
        }

        /// <summary>
        /// Get the active claim condition
        /// </summary>
        public async Task<ClaimConditions> GetActive()
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<ClaimConditions>(getRoute("getActive"), new string[] { });
            }
            else
            {
                var id = await TransactionManager.ThirdwebRead<DropERC20Contract.GetActiveClaimConditionIdFunction, DropERC20Contract.GetActiveClaimConditionIdOutputDTO>(
                    contractAddress,
                    new DropERC20Contract.GetActiveClaimConditionIdFunction() { }
                );

                var data = await TransactionManager.ThirdwebRead<DropERC20Contract.GetClaimConditionByIdFunction, DropERC20Contract.GetClaimConditionByIdOutputDTO>(
                    contractAddress,
                    new DropERC20Contract.GetClaimConditionByIdFunction() { ConditionId = id.ReturnValue1 }
                );

                var currency = new Currency();
                try
                {
                    currency = await ThirdwebManager.Instance.SDK.GetContract(data.Condition.Currency).ERC20.Get();
                }
                catch
                {
                    ThirdwebDebug.Log("Could not fetch currency metadata, proceeding without it.");
                }

                return new ClaimConditions()
                {
                    availableSupply = (data.Condition.MaxClaimableSupply - data.Condition.SupplyClaimed).ToString(),
                    currencyAddress = data.Condition.Currency,
                    currencyMetadata = new CurrencyValue(
                        currency.name,
                        currency.symbol,
                        currency.decimals,
                        data.Condition.PricePerToken.ToString(),
                        data.Condition.PricePerToken.ToString().FormatERC20(4, int.Parse(currency.decimals), true)
                    ),
                    currentMintSupply = data.Condition.SupplyClaimed.ToString(),
                    maxClaimablePerWallet = data.Condition.QuantityLimitPerWallet.ToString(),
                    maxClaimableSupply = data.Condition.MaxClaimableSupply.ToString(),
                };
            }
        }

        /// <summary>
        /// Check whether the connected wallet is eligible to claim
        /// </summary>
        public async Task<bool> CanClaim(string quantity, string? addressToCheck = null)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<bool>(getRoute("canClaim"), Utils.ToJsonStringArray(quantity, addressToCheck));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Get the reasons why the connected wallet is not eligible to claim
        /// </summary>
        public async Task<string[]> GetIneligibilityReasons(string quantity, string? addressToCheck = null)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<string[]>(getRoute("getClaimIneligibilityReasons"), Utils.ToJsonStringArray(quantity, addressToCheck));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }

        /// <summary>
        /// Get the special values set in the allowlist for the given wallet
        /// </summary>
        public async Task<bool> GetClaimerProofs(string claimerAddress)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<bool>(getRoute("getClaimerProofs"), Utils.ToJsonStringArray(claimerAddress));
            }
            else
            {
                throw new UnityException("This functionality is not yet available on your current platform.");
            }
        }
    }

    /// <summary>
    /// Generate, verify and mint signed mintable payloads
    /// </summary>
    public class ERC20Signature : Routable
    {
        private readonly string contractAddress;

        /// <summary>
        /// Generate, verify and mint signed mintable payloads
        /// </summary>
        public ERC20Signature(string parentRoute, string contractAddress)
            : base(Routable.append(parentRoute, "signature"))
        {
            this.contractAddress = contractAddress;
        }

        /// <summary>
        /// Generate a signed mintable payload. Requires minting permission.
        /// </summary>
        public async Task<ERC20SignedPayload> Generate(ERC20MintPayload payloadToSign, string privateKeyOverride = "")
        {
            if (Utils.IsWebGLBuild())
            {
                var signedPayload = await Bridge.InvokeRoute<ERC20SignedPayload>(getRoute("generate"), Utils.ToJsonStringArray(payloadToSign));

                if (privateKeyOverride == "")
                    return signedPayload;

                var name = await ThirdwebManager.Instance.SDK.GetContract(contractAddress).Read<string>("name");
                var req = new TokenERC20Contract.MintRequest()
                {
                    To = payloadToSign.to,
                    PrimarySaleRecipient = signedPayload.payload.primarySaleRecipient,
                    Quantity = BigInteger.Parse(payloadToSign.quantity.ToWei()),
                    Price = BigInteger.Parse(payloadToSign.price.ToWei()),
                    Currency = payloadToSign.currencyAddress,
                    ValidityStartTimestamp = payloadToSign.mintStartTime,
                    ValidityEndTimestamp = payloadToSign.mintEndTime,
                    Uid = payloadToSign.uid.HexStringToByteArray()
                };
                string signature = await Thirdweb.EIP712.GenerateSignature_TokenERC20(name, "1", await ThirdwebManager.Instance.SDK.wallet.GetChainId(), contractAddress, req, privateKeyOverride);

                signedPayload = new ERC20SignedPayload()
                {
                    signature = signature,
                    payload = new ERC20SignedPayloadOutput()
                    {
                        to = req.To,
                        price = req.Price.ToString(),
                        currencyAddress = req.Currency,
                        primarySaleRecipient = req.PrimarySaleRecipient,
                        quantity = req.Quantity.ToString(),
                        uid = req.Uid.ByteArrayToHexString(),
                        mintStartTime = (long)req.ValidityStartTimestamp,
                        mintEndTime = (long)req.ValidityEndTimestamp
                    }
                };

                return signedPayload;
            }
            else
            {
                var primarySaleRecipient = await TransactionManager.ThirdwebRead<TokenERC20Contract.PrimarySaleRecipientFunction, TokenERC20Contract.PrimarySaleRecipientOutputDTO>(
                    contractAddress,
                    new TokenERC20Contract.PrimarySaleRecipientFunction() { }
                );

                var req = new TokenERC20Contract.MintRequest()
                {
                    To = payloadToSign.to,
                    PrimarySaleRecipient = primarySaleRecipient.ReturnValue1,
                    Quantity = BigInteger.Parse(payloadToSign.quantity.ToWei()),
                    Price = BigInteger.Parse(payloadToSign.price.ToWei()),
                    Currency = payloadToSign.currencyAddress,
                    ValidityStartTimestamp = payloadToSign.mintStartTime,
                    ValidityEndTimestamp = payloadToSign.mintEndTime,
                    Uid = payloadToSign.uid.HexStringToByteArray()
                };

                var name = await TransactionManager.ThirdwebRead<TokenERC20Contract.NameFunction, TokenERC20Contract.NameOutputDTO>(contractAddress, new TokenERC20Contract.NameFunction() { });

                string signature = await Thirdweb.EIP712.GenerateSignature_TokenERC20(
                    name.ReturnValue1,
                    "1",
                    await ThirdwebManager.Instance.SDK.wallet.GetChainId(),
                    contractAddress,
                    req,
                    string.IsNullOrEmpty(privateKeyOverride) ? null : privateKeyOverride
                );

                var signedPayload = new ERC20SignedPayload()
                {
                    signature = signature,
                    payload = new ERC20SignedPayloadOutput()
                    {
                        to = req.To,
                        price = req.Price.ToString(),
                        currencyAddress = req.Currency,
                        primarySaleRecipient = req.PrimarySaleRecipient,
                        quantity = req.Quantity.ToString(),
                        uid = req.Uid.ByteArrayToHexString(),
                        mintStartTime = (long)req.ValidityStartTimestamp,
                        mintEndTime = (long)req.ValidityEndTimestamp
                    }
                };

                return signedPayload;
            }
        }

        /// <summary>
        /// Verify that a signed mintable payload is valid
        /// </summary>
        public async Task<bool> Verify(ERC20SignedPayload signedPayload)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<bool>(getRoute("verify"), Utils.ToJsonStringArray(signedPayload));
            }
            else
            {
                var verifyResult = await TransactionManager.ThirdwebRead<TokenERC20Contract.VerifyFunction, TokenERC20Contract.VerifyOutputDTO>(
                    contractAddress,
                    new TokenERC20Contract.VerifyFunction()
                    {
                        Req = new TokenERC20Contract.MintRequest()
                        {
                            To = signedPayload.payload.to,
                            PrimarySaleRecipient = signedPayload.payload.primarySaleRecipient,
                            Quantity = BigInteger.Parse(signedPayload.payload.quantity),
                            Price = BigInteger.Parse(signedPayload.payload.price),
                            Currency = signedPayload.payload.currencyAddress,
                            ValidityStartTimestamp = signedPayload.payload.mintStartTime,
                            ValidityEndTimestamp = signedPayload.payload.mintEndTime,
                            Uid = signedPayload.payload.uid.HexStringToByteArray()
                        },
                        Signature = signedPayload.signature.HexStringToByteArray()
                    }
                );
                return verifyResult.ReturnValue1;
            }
        }

        /// <summary>
        /// Mint a signed mintable payload
        /// </summary>
        public async Task<TransactionResult> Mint(ERC20SignedPayload signedPayload)
        {
            if (Utils.IsWebGLBuild())
            {
                return await Bridge.InvokeRoute<TransactionResult>(getRoute("mint"), Utils.ToJsonStringArray(signedPayload));
            }
            else
            {
                return await TransactionManager.ThirdwebWrite(
                    contractAddress,
                    new TokenERC20Contract.MintWithSignatureFunction()
                    {
                        Req = new TokenERC20Contract.MintRequest()
                        {
                            To = signedPayload.payload.to,
                            PrimarySaleRecipient = signedPayload.payload.primarySaleRecipient,
                            Quantity = BigInteger.Parse(signedPayload.payload.quantity),
                            Price = BigInteger.Parse(signedPayload.payload.price),
                            Currency = signedPayload.payload.currencyAddress,
                            ValidityStartTimestamp = signedPayload.payload.mintStartTime,
                            ValidityEndTimestamp = signedPayload.payload.mintEndTime,
                            Uid = signedPayload.payload.uid.HexStringToByteArray()
                        },
                        Signature = signedPayload.signature.HexStringToByteArray()
                    },
                    BigInteger.Parse(signedPayload.payload.quantity) * BigInteger.Parse(signedPayload.payload.price)
                );
            }
        }
    }
}
