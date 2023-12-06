# 1.0.0 (2023-12-06)


### Bug Fixes

* bake-buffer is not displayed in inspector ([6a5d5a2](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/6a5d5a2d91756878603c58dfd248de724c927e75))
* CompositeCanvasEffect has 'DisallowMultipleComponent' attribute ([2ca5a91](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/2ca5a913d51aae03411620b99455de0f0bd22cf2))
* delayed extra callback ([6cccbe4](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/6cccbe48906a7cf95cbc61c2cec07e5eb554418d))
* difficult to see when alpha is small ([3c85252](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/3c852525fc3119e3b2df4884e73964df5bd46312))
* fix baking process ([b7ce244](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/b7ce2445bd29f5cafd61b222f026bb95ad79508c))
* fix compile error ([9dc84e6](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/9dc84e6ddfffde56b627b64d15b3150987ed4cb0))
* fix typo 'extends' to 'extents' ([6428cfe](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/6428cfe3173fb0bbb4ed8bd4c4d76d1e182e5123))
* ignore disabled source graphics when baking ([0812119](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/081211929ae47e320015bc9b7fa58663b4b9617b))
* material fix ([374a933](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/374a933d1d686de3b50aa6aac80e6c29580663a4))
* mesh error ([2a6a316](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/2a6a316edb1ff41f9793e1e9b064674775d14179))
* missing zw component when copying mesh ([59215e2](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/59215e2d9fdf13aa615a5e2a8c3583c7b60b552f))
* nested CanvasGroups make bake-buffer alpha smaller ([f9eff35](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/f9eff35d7396faf381df1b3a407d63fd3ea91386))
* remove global cache ([c33f2f6](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/c33f2f636c4b96e94c27a413bbcf6ce71d99beb7))
* Remove unnecessary override property ([3b01dff](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/3b01dff90f026dd29260d28b40976bf2735c522c))
* return intList ([9394994](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/939499495eebd52a16341d66daec552bf6c5dcf0))
* rotation is ignored in orthographic mode. ([9747fc1](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/9747fc1e8ebabc20fa07c4e65706ee55947bdb7f))
* shared TextMeshPro material is not used ([a94d4d0](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/a94d4d0ea5134c5398ac733d474518af46d6a5e2))
* skip blur/cutoff phase if possible ([c3379dd](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/c3379dd78604cfd1d8f61559c85d0452a62458b3))
* source graphics with `scale.z = 0` will be not baked ([c145db4](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/c145db456bcbacb320f4e16151761504603ec2eb))
* support CanvasGroup.alpha for source graphic ([5995eea](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/5995eea89906e7caf55437215e1943cbe7b125e2))


### Features

* add 'bakingTrigger' option (automatic, manually, always, on enable) ([d2f3d34](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/d2f3d348d81e2906c953f2d2267fa2ced8cc2cfe))
* add `ignoreSelf` and `ignoreChildren` options for source ([2f8ccfb](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/2f8ccfb623b6bee44d649326f6b59341e9109027))
* add blur effect ([8d5df19](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/8d5df1923313c17fd570f285d2ffd683a127cd92))
* add CompositeCanvasRenderer ([acc0803](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/acc0803f857618ffb30f154d4798d50ee74dac18))
* add glow and bloom effects ([4d4b441](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/4d4b441fa042b10333728dd065c3eefac79cc7d4))
* add group id to share the bake buffer ([53f03a4](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/53f03a488c5f864e6503ad4d04823c80843ade1c))
* add mirror effect ([44df1cb](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/44df1cb5754241c17fd19e2becab9f46bb704e29))
* add public 'orthographic' property ([175eea9](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/175eea920379ffb497457b4e838f91b851eb4dcb))
* add relative mode (orthographic mode) ([67aadba](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/67aadbaae9f3ab9519d00cbe857345b88e9460c5))
* add shadow and outline effects ([04c0f3f](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/04c0f3f25ed72a68e42883509ed0811306c1839d))
* editor update ([1a98bc4](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/1a98bc463cbf4ff8ddb8e2ccc14230711ccef9f3))
* enable/disable culling for baking ([d2cb243](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/d2cb24366508edf94e1eebef6cf96117308a5c8e))
* enable/disable upscaling buffer ([e1008f2](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/e1008f22788e62a595833dffecc97da666bfcb4a))
* gizmo for orthographic baking ([d5bed5a](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/d5bed5a9e3596bbbb487e6824c3040506a602c63))
* global culling ([be0c648](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/be0c64897f9e5a605246aedb343932f42f22c9bc))
* skip baking when `ColorMask=0` (for masking) ([5bc7064](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/5bc7064915a21dd51bd4f89c3c490551dff4aafd))
* support IMeshModifier.ModifyMesh(Mesh) ([39dedd7](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/39dedd731988297f79fade05beb79398dd952ba2))
* support Mask for baking ([8015dcc](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/8015dcc94894db8bb1bbb13eb5463a7d5c617b4c))
* support nested CompositeCanvasRenderer ([99121f2](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/99121f2b89339c20698acb98834126fab8d29096))
* TMP outline, glow, underlay support ([e5abfa8](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/e5abfa8f3e2c10a18857ffc6d2bc6de1ab7c3b5e))
* TMP performance improve ([cebe9fc](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/cebe9fce8736891f74197bd000e26463ebe13386))


### Performance Improvements

* remove alloc code ([35c671b](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/35c671bb130b345ce53b9a8035e01a0372676f5a))

# [1.0.0-preview.2](https://github.com/mob-sakai/CompositeCanvasRenderer/compare/1.0.0-preview.1...1.0.0-preview.2) (2023-12-01)


### Bug Fixes

* bake-buffer is not displayed in inspector ([6a5d5a2](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/6a5d5a2d91756878603c58dfd248de724c927e75))
* CompositeCanvasEffect has 'DisallowMultipleComponent' attribute ([2ca5a91](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/2ca5a913d51aae03411620b99455de0f0bd22cf2))
* delayed extra callback ([6cccbe4](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/6cccbe48906a7cf95cbc61c2cec07e5eb554418d))
* difficult to see when alpha is small ([3c85252](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/3c852525fc3119e3b2df4884e73964df5bd46312))
* fix baking process ([b7ce244](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/b7ce2445bd29f5cafd61b222f026bb95ad79508c))
* fix compile error ([9dc84e6](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/9dc84e6ddfffde56b627b64d15b3150987ed4cb0))
* fix typo 'extends' to 'extents' ([6428cfe](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/6428cfe3173fb0bbb4ed8bd4c4d76d1e182e5123))
* material fix ([374a933](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/374a933d1d686de3b50aa6aac80e6c29580663a4))
* mesh error ([2a6a316](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/2a6a316edb1ff41f9793e1e9b064674775d14179))
* missing zw component when copying mesh ([59215e2](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/59215e2d9fdf13aa615a5e2a8c3583c7b60b552f))
* nested CanvasGroups make bake-buffer alpha smaller ([f9eff35](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/f9eff35d7396faf381df1b3a407d63fd3ea91386))
* remove global cache ([c33f2f6](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/c33f2f636c4b96e94c27a413bbcf6ce71d99beb7))
* Remove unnecessary override property ([3b01dff](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/3b01dff90f026dd29260d28b40976bf2735c522c))
* return intList ([9394994](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/939499495eebd52a16341d66daec552bf6c5dcf0))
* rotation is ignored in orthographic mode. ([9747fc1](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/9747fc1e8ebabc20fa07c4e65706ee55947bdb7f))
* shared TextMeshPro material is not used ([a94d4d0](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/a94d4d0ea5134c5398ac733d474518af46d6a5e2))
* skip blur/cutoff phase if possible ([c3379dd](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/c3379dd78604cfd1d8f61559c85d0452a62458b3))
* source graphics with `scale.z = 0` will be not baked ([c145db4](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/c145db456bcbacb320f4e16151761504603ec2eb))
* support CanvasGroup.alpha for source graphic ([5995eea](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/5995eea89906e7caf55437215e1943cbe7b125e2))


### Features

* add 'bakingTrigger' option (automatic, manually, always, on enable) ([d2f3d34](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/d2f3d348d81e2906c953f2d2267fa2ced8cc2cfe))
* add `ignoreSelf` and `ignoreChildren` options for source ([2f8ccfb](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/2f8ccfb623b6bee44d649326f6b59341e9109027))
* add public 'orthographic' property ([175eea9](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/175eea920379ffb497457b4e838f91b851eb4dcb))
* add relative mode (orthographic mode) ([67aadba](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/67aadbaae9f3ab9519d00cbe857345b88e9460c5))
* editor update ([1a98bc4](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/1a98bc463cbf4ff8ddb8e2ccc14230711ccef9f3))
* enable/disable culling for baking ([d2cb243](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/d2cb24366508edf94e1eebef6cf96117308a5c8e))
* enable/disable upscaling buffer ([e1008f2](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/e1008f22788e62a595833dffecc97da666bfcb4a))
* gizmo for orthographic baking ([d5bed5a](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/d5bed5a9e3596bbbb487e6824c3040506a602c63))
* global culling ([be0c648](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/be0c64897f9e5a605246aedb343932f42f22c9bc))
* skip baking when `ColorMask=0` (for masking) ([5bc7064](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/5bc7064915a21dd51bd4f89c3c490551dff4aafd))
* support IMeshModifier.ModifyMesh(Mesh) ([39dedd7](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/39dedd731988297f79fade05beb79398dd952ba2))
* support Mask for baking ([8015dcc](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/8015dcc94894db8bb1bbb13eb5463a7d5c617b4c))
* TMP outline, glow, underlay support ([e5abfa8](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/e5abfa8f3e2c10a18857ffc6d2bc6de1ab7c3b5e))
* TMP performance improve ([cebe9fc](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/cebe9fce8736891f74197bd000e26463ebe13386))

# 1.0.0-preview.1 (2023-10-26)


### Features

* add blur effect ([8d5df19](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/8d5df1923313c17fd570f285d2ffd683a127cd92))
* add CompositeCanvasRenderer ([acc0803](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/acc0803f857618ffb30f154d4798d50ee74dac18))
* add glow and bloom effects ([4d4b441](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/4d4b441fa042b10333728dd065c3eefac79cc7d4))
* add mirror effect ([44df1cb](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/44df1cb5754241c17fd19e2becab9f46bb704e29))
* add shadow and outline effects ([04c0f3f](https://github.com/mob-sakai/CompositeCanvasRenderer/commit/04c0f3f25ed72a68e42883509ed0811306c1839d))
