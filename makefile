CONFIG?=Release
PREFIX?=prefix
PREFIX:=$(abspath $(PREFIX))
VERSION=1.0.20414
PROJECT=Soapbox
STAGEDIR=Scratch/Homebrew
libFiles=$(PROJECT).exe \
	Api.ServiceModel.dll \
	JWT.dll \
	MongoDB.Bson.dll \
	MongoDB.Driver.dll \
	Rql.dll \
	Rql.MongoDB.dll \
	ServiceBelt.dll \
	ServiceStack.Client.dll \
	ServiceStack.Common.dll \
	ServiceStack.dll \
	ServiceStack.Interfaces.dll \
	ServiceStack.Text.dll \
	ToolBelt.dll \
	TsonLibrary.dll
supportFiles=makefile README.md LICENSE.md template.sh
lc=$(shell echo $(1) | tr A-Z a-z)
zipFile=$(PROJECT)-$(VERSION).tar.gz

define copyRule
$(1): $(2)
	cp $$< $$@

endef

define mkDirRule
$(1):
	mkdir -p $$@

endef

.PHONY: default
default:
	$(error Specify clean, dist or install)

.PHONY: dist
dist: $(STAGEDIR) \
	  $(STAGEDIR)/lib/$(PROJECT) \
	  $(foreach X,$(libFiles),$(STAGEDIR)/lib/$(PROJECT)/$(X)) \
	  $(foreach X,$(supportFiles),$(STAGEDIR)/$(X)) \
	  $(zipFile)

$(eval $(call mkDirRule,$(STAGEDIR)))
$(eval $(call mkDirRule,$(STAGEDIR)/lib/$(PROJECT)))

$(zipFile): $(foreach X,$(libFiles),$(STAGEDIR)/lib/$(PROJECT)/$(X)) \
			$(foreach X,$(supportFiles),$(STAGEDIR)/$(X))
	tar -cvz -C $(STAGEDIR) -f $(zipFile) ./
	openssl sha1 $(zipFile)
	@echo "aws s3 cp" $(zipFile) "s3://jlyonsmith/ --profile jamoki --acl public-read"

$(foreach X,$(libFiles),$(eval $(call copyRule,$(STAGEDIR)/lib/$(PROJECT)/$(X),$(PROJECT)/bin/$(CONFIG)/$(X))))
$(foreach X,$(supportFiles),$(eval $(call copyRule,$(STAGEDIR)/$(X),$(X))))

# NOTE: Test 'install' by going to STAGEDIR and running there!

.PHONY: install
install: $(PREFIX)/bin \
		 $(PREFIX)/lib/$(PROJECT) \
		 $(foreach X,$(libFiles),$(PREFIX)/lib/$(PROJECT)/$(X)) \
		 $(foreach X,$(supportFiles),$(PREFIX)/$(X)) \
		 $(PREFIX)/bin/$(call lc,$(PROJECT))
		 
$(eval $(call mkDirRule,$(PREFIX)/lib/$(PROJECT)))
$(eval $(call mkDirRule,$(PREFIX)/bin))

$(PREFIX)/bin/$(call lc,$(PROJECT)): $(PREFIX)/template.sh
	sed -e 's,_PROJECT_,$(PROJECT),g' -e 's,_PREFIX_,$(PREFIX),g' template.sh > $@
	chmod u+x $@

$(foreach X,$(libFiles),$(eval $(call copyRule,$(PREFIX)/lib/$(PROJECT)/$(X),lib/$(PROJECT)/$(X))))
$(foreach X,$(supportFiles),$(eval $(call copyRule,$(PREFIX)/$(X),$(X))))

.PHONY: clean
clean:
	-@rm *.gz
	-@rm -rf $(STAGEDIR)
