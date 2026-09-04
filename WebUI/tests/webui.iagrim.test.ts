import { test, expect } from '@playwright/test';

test('WebUI detects an IAGrim host', async ({ page }) => {
    await page.addInitScript(() => {
        window.invokeCSharpAction = () => {};
        window.iagrim = {
            SignalReady: () => {},
            RequestMoreItems: () => {},
            RequestCollectionData: () => {},
            TransferItem: () => '',
            SetClipboard: () => {},
            GetItemSetAssociations: () => '[]',
            GetBackedUpCharacters: () => '[]',
            GetCharacterDownloadUrl: () => '{}',
            OpenURL: () => {},
            GetTranslationStrings: () => ({}),
            DismissNumericFilterBanner: () => {},
        };
    });

    await page.goto('/');

    const isEmbedded = await page.evaluate(() => {
        return window.iagrim !== undefined;
    });

    expect(isEmbedded).toBe(true);
});

test('WebUI signals IAGrim when loaded', async ({ page }) => {
    await page.addInitScript(() => {
        window.invokeCSharpAction = () => {};
        window.iagrim = {
            SignalReady: () => {
                (window as any).__signalReadyCalled = true;
            },
            RequestMoreItems: () => {},
            RequestCollectionData: () => {},
            TransferItem: () => '',
            SetClipboard: () => {},
            GetItemSetAssociations: () => '[]',
            GetBackedUpCharacters: () => '[]',
            GetCharacterDownloadUrl: () => '{}',
            OpenURL: () => {},
            GetTranslationStrings: () => ({}),
            DismissNumericFilterBanner: () => {},
        };
    });

    await page.goto('/');

    await expect.poll(async () => {
        return page.evaluate(() => {
            return (window as any).__signalReadyCalled === true;
        });
    }).toBe(true);
});

test('IAGrim can send items to the WebUI', async ({ page }) => {
    await page.addInitScript(() => {
        window.invokeCSharpAction = () => {};
        window.iagrim = {
            SignalReady: () => {
                (window as any).__signalReadyCalled = true;
            },
            RequestMoreItems: () => {},
            RequestCollectionData: () => {},
            TransferItem: () => JSON.stringify({ success: true }),
            SetClipboard: () => {},
            GetItemSetAssociations: () => '[]',
            GetBackedUpCharacters: () => '[]',
            GetCharacterDownloadUrl: () => '{}',
            OpenURL: () => {},
            GetTranslationStrings: () => ({}),
            DismissNumericFilterBanner: () => {},
        };
    });

    await page.goto('/');

    // Wait until App.componentDidMount() has run and window.message has been installed.
    await expect.poll(async () => {
        return page.evaluate(() => {
            return (window as any).__signalReadyCalled === true;
        });
    }).toBe(true);

    await page.evaluate(() => {
        window.message({
            type: 5, // IOMessageType.SetItems
            data: {
                replaceExistingItems: true,
                items: [
                    [
                        {
                            uniqueIdentifier: 'PI/123/test-item',
                            mergeIdentifier: 'test-item',
                            baseRecord: 'records/items/test_item.dbr',
                            icon: 'test_item.png',
                            quality: 'Rare',
                            name: 'Test Item',
                            socket: '',
                            level: 1,
                            url: ['PI', '123', 'test-item'],
                            type: 2, // Player
                            hasRecipe: false,
                            greenRarity: 0,
                            headerStats: [],
                            bodyStats: [],
                            petStats: [],
                            isHardcore: false,
                            replicaStats: [],
                        },
                    ],
                ],
                numItemsFound: 1,
                numItemsApproximate: false,
                hasMore: false,
            },
        });
    });

    await expect(page.getByText('Test Item')).toBeVisible();
});

test('WebUI can transfer an item through IAGrim', async ({ page }) => {
    await page.addInitScript(() => {
        window.invokeCSharpAction = () => {};
        window.iagrim = {
            SignalReady: () => {
                (window as any).__signalReadyCalled = true;
            },
            RequestMoreItems: () => {},
            RequestCollectionData: () => {},
            TransferItem: (url: object[], transferAll: boolean) => {
                (window as any).__transferItem = {
                    url,
                    transferAll,
                };
                return JSON.stringify({ success: true });
            },
            SetClipboard: () => {},
            GetItemSetAssociations: () => '[]',
            GetBackedUpCharacters: () => '[]',
            GetCharacterDownloadUrl: () => '{}',
            OpenURL: () => {},
            GetTranslationStrings: () => ({}),
            DismissNumericFilterBanner: () => {},
        };
    });

    await page.goto('/');

    // Wait until App.componentDidMount() has run.
    await expect.poll(async () => {
        return page.evaluate(() => {
            return (window as any).__signalReadyCalled === true;
        });
    }).toBe(true);

    await page.evaluate(() => {
        window.message({
            type: 5, // IOMessageType.SetItems
            data: {
                replaceExistingItems: true,
                items: [
                    [
                        {
                            uniqueIdentifier: 'PI/123/test-item',
                            mergeIdentifier: 'test-item',
                            baseRecord: 'records/items/test_item.dbr',
                            icon: 'test_item.png',
                            quality: 'Rare',
                            name: 'Transfer Test Item',
                            socket: '',
                            level: 1,
                            url: ['PI', '123', 'test-item'],
                            type: 2, // Player
                            hasRecipe: false,
                            greenRarity: 0,
                            headerStats: [],
                            bodyStats: [],
                            petStats: [],
                            isHardcore: false,
                            replicaStats: [],
                        },
                    ],
                ],
                numItemsFound: 1,
                numItemsApproximate: false,
                hasMore: false,
            },
        });
    });

    await expect(page.getByText('Transfer Test Item')).toBeVisible();
    await page.locator('.item .link-container a').click();

    await expect.poll(async () => {
        return page.evaluate(() => (window as any).__transferItem);
    }).toEqual({
        url: ['PI', '123', 'test-item', '-', '-', '-'],
        transferAll: false,
    });
});

